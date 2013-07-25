using SharpDX;

using System;
using System.Linq;
using System.Collections.Generic;

namespace MST_Heightmap_Generator_GUI
{
    using SharpDX.Toolkit.Graphics;
    

    public class TerrainRenderingPreview : WPFHost.IScene
    {
        public bool DeactivateRendering { get; set; }

        private WPFHost.ISceneHost host;
        public GraphicsDevice GraphicsDevice { get; private set; }

        private ShaderAutoReload terrainShader;
        private ShaderAutoReload skyShader;
        private ShaderAutoReload computeRelaxedConeShader;

        private VertexInputLayout sphereVertexInputLayout;
        private ShaderAutoReload sphereBillboardShader;

        private List<PointSet> pointSets = new List<PointSet>();
        private PointSet pointSetWithSelection = null;

        private Camera camera;
        
        #region heightmap properties

        private Texture heightmapTexture;
        private float heightmapPixelPerWorldUnit = 1; // some default values for convinience
        private float terrainScale = 20;
        private Vector3 terrainTranslation = new Vector3(-128,0,-128);

        #endregion

        #region Sky

        public float TimeOfDay
        {
            get
            {
                return timeOfDay;
            }
            set
            {
                timeOfDay = value;
                if (terrainShader != null)
                {
                    lightDirection = new Vector3((float)Math.Cos(Math.PI * timeOfDay), (float)Math.Sin(Math.PI * timeOfDay), 0);
                    terrainShader.Effect.Parameters["LightDirection"].SetValue(lightDirection);
                    ReGenerateSkyCubeMap();
                }
            }
        }
        private float timeOfDay = 0.3f;
        private Vector3 lightDirection;

        const int CUBEMAP_RES = 512;
        private RenderTargetCube skyCubemap;

        #endregion

        #region Conemap Processing

        Texture2D tempSingleChannelHeightmap;
        Texture2D tempSingleChannelCones;
        Stack<Vector2> conemapProcessingWorkItems = new Stack<Vector2>();

        #endregion 

        private SamplerState linearBorderSamplerState;


        public bool Closing { get; set; }
        private bool resizeNeeded = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
            DeactivateRendering = false;
        }

        #region PointSet Functions

        /// <summary>
        /// Removes all PointSet.
        /// </summary>
        public void ClearPointSet()
        {
            pointSets.Clear();
        }
        /// <summary>
        /// Adds a PointSet.
        /// </summary>
        /// <param name="newPointSet">New point set</param>
        public void AddPointSet(PointSet newPointSet)
        {
            newPointSet.InitGraphicsRessource(GraphicsDevice);
            pointSets.Add(newPointSet);
        }
        /// <summary>
        /// Removes a PointSet.
        /// </summary>
        /// <param name="pointSet">PointSet to remove</param>
        public void RemovePointSet(PointSet pointSet)
        {
            pointSets.Remove(pointSet);
        }

        #endregion


        /// <summary>
        /// The visual output can be rescaled without regeneration of the map.
        /// </summary>
        /// <param name="terrainScale">Scale factor</param>
        public void SetScaleFactor(float terrainScale)
        {
            this.terrainScale = terrainScale;

            if (terrainShader != null)
            {
                var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
                heightmapConstantBuffer.Parameters["TerrainScale"].SetValue(terrainScale);
                heightmapConstantBuffer.IsDirty = true;
            }

            if(sphereBillboardShader != null)
                sphereBillboardShader.Effect.Parameters["HeightScale"].SetValue(terrainScale);
        }

        private void DoConemapTask()
        {
            if (conemapProcessingWorkItems.Count > 0)
            {
                // compute another fragment
                Vector2 textureAreaMin = conemapProcessingWorkItems.Pop();

                computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Compute"];

                computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(tempSingleChannelHeightmap);
                computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource(tempSingleChannelCones);

                computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();

                computeRelaxedConeShader.Effect.Parameters["TextureAreaMin"].SetValue<int>(new int[] { (int)textureAreaMin.X, (int)textureAreaMin.Y });
                computeRelaxedConeShader.Effect.ConstantBuffers[0].Update();
                GraphicsDevice.Dispatch(tempSingleChannelHeightmap.Width / 32, tempSingleChannelHeightmap.Height / 32, 1);

                // combine
                computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(false);
                CombineTempMapsToCombined();

                // cleanup
                if (conemapProcessingWorkItems.Count == 0)
                {
                    tempSingleChannelHeightmap.Dispose();
                    tempSingleChannelCones.Dispose();
                }
            }
        }

        private void CombineTempMapsToCombined()
        {
            terrainShader.Effect.Parameters["Heightmap"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);

            computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Combine"];
            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(tempSingleChannelHeightmap);
            computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource(tempSingleChannelCones);
            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource(heightmapTexture);

            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Dispatch(tempSingleChannelHeightmap.Width / 32, tempSingleChannelHeightmap.Height / 32, 1);
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(true);

            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
        }

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit)
        {
            this.heightmapPixelPerWorldUnit = heightmapPixelPerWorldUnit;

            // detach old textures
            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            terrainShader.Effect.Parameters["Heightmap"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);

            // remove old textures
            if(heightmapTexture != null)
                heightmapTexture.Dispose();
            if (tempSingleChannelHeightmap != null)
                tempSingleChannelHeightmap.Dispose();
            if (tempSingleChannelCones != null)
                tempSingleChannelCones.Dispose();

            // generate jobs
            conemapProcessingWorkItems.Clear();
            const int TILE_SIZE = 32;   // if you change this value, you also have to change " AreaPerCall in computerelaxedconemap.fx
            for (int x = 0; x < heightmap.GetLength(0); x += TILE_SIZE)
            {
                for (int y = 0; y < heightmap.GetLength(1); y += TILE_SIZE)
                    conemapProcessingWorkItems.Push(new Vector2(x, y));
            }
            

            // create temp-textures
            tempSingleChannelHeightmap = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource);
            tempSingleChannelHeightmap.SetData<float>(heightmap.Cast<float>().ToArray());
            tempSingleChannelCones = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            GraphicsDevice.Clear(tempSingleChannelCones, new Color4(2.0f));
            
            // compose to heightmap texture
            heightmapTexture = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R16G16.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            CombineTempMapsToCombined();

            // setup heightmap cbuffer
            SetupTerrainConstants();
        }

        private void ReGenerateSkyCubeMap()
        {
            SharpDX.Direct3D11.DepthStencilView depthStencilBefore;
            var renderTargetsBefore = GraphicsDevice.GetRenderTargets(out depthStencilBefore);

            skyShader.Effect.Parameters["LightDirection"].SetValue(lightDirection);
            GraphicsDevice.SetRenderTargets(skyCubemap.RenderTargetView[ViewType.Full, 0, 0]);
            skyShader.Effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.PointList, 1);

            GraphicsDevice.SetRenderTargets(depthStencilBefore, renderTargetsBefore);
        }

        private void SetupTerrainConstants()
        {
            var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
            heightmapConstantBuffer.Parameters["HeightmapSize"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            heightmapConstantBuffer.Parameters["HeightmapSizeInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapSizeInv
            heightmapConstantBuffer.Parameters["WorldUnitToHeightmapTexcoord"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height) * heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.Parameters["HeightmapPixelSizeInWorld"].SetValue(new Vector2(1.0f / heightmapPixelPerWorldUnit));
            heightmapConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["TerrainHeightmapSampler"].SetResource(linearBorderSamplerState);
            terrainShader.Effect.Parameters["SkyCubemap"].SetResource(skyCubemap);
            terrainShader.Effect.Parameters["CubemapSampler"].SetResource(GraphicsDevice.SamplerStates.LinearWrap);  

            terrainTranslation = new Vector3(-heightmapTexture.Width * 0.5f / heightmapPixelPerWorldUnit, 0, -heightmapTexture.Height * 0.5f / heightmapPixelPerWorldUnit);
            sphereBillboardShader.Effect.Parameters["Translation"].SetValue(terrainTranslation);

            SetScaleFactor(terrainScale);
        }

        void WPFHost.IScene.Attach(WPFHost.ISceneHost host)
        {
            this.host = host;
            camera = new Camera((float)host.RenderTargetWidth / host.RenderTargetHeight, (float)(75.0 * Math.PI / 180.0), 0.1f, 10000.0f);

            // device setup
            if (host.Device == null)
                throw new Exception("Scene host device is null");
            GraphicsDevice = GraphicsDevice.New(host.Device);
            RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(GraphicsDevice, host.RenderTargetView, true);
            GraphicsDevice.Presenter = new RenderTargetGraphicsPresenter(GraphicsDevice, backbufferRenderTarget);
            GraphicsDevice.SetRenderTargets(backbufferRenderTarget);
            
 
            // load shader
            computeRelaxedConeShader = new ShaderAutoReload("computerelaxedconemap.fx", GraphicsDevice);
            terrainShader = new ShaderAutoReload("terrain.fx", GraphicsDevice);
            sphereBillboardShader = new ShaderAutoReload("spherebillboards.fx", GraphicsDevice);
            terrainShader.OnReload += SetupTerrainConstants;
            terrainShader.OnReload += () => SetScaleFactor(terrainScale);

            // linear sampler
            var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
            samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear;
            samplerStateDesc.BorderColor = Color4.Black;
            linearBorderSamplerState = SamplerState.New(GraphicsDevice, "TerrainHeightmapSampler", samplerStateDesc);

            // dummy heightmap
            heightmapTexture = Texture2D.New(GraphicsDevice, 1, 1, MipMapCount.Auto, PixelFormat.R32.Float);
            heightmapTexture.SetData<float>(new float[] { 0.0f });

            // vertex input layout
            sphereVertexInputLayout = VertexInputLayout.New(VertexBufferLayout.New(0, VertexElement.Position(SharpDX.DXGI.Format.R32G32B32_Float)));

            // generate sky
            skyShader = new ShaderAutoReload("sky.fx", GraphicsDevice);
            skyShader.OnReload += ReGenerateSkyCubeMap;
            skyCubemap = RenderTargetCube.New(GraphicsDevice, CUBEMAP_RES, 0, PixelFormat.R8G8B8A8.SNorm);
            TimeOfDay = timeOfDay;

            // constant buffer to defaults
            SetupTerrainConstants();
        }

        void WPFHost.IScene.Detach()
        {
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
        }

        void WPFHost.IScene.Update(TimeSpan sceneTime)
        {
            camera.Update((float)sceneTime.TotalSeconds);

            lock (this)
            {
                if (resizeNeeded)
                {
                    camera.AspectRatio = (float)host.RenderTargetWidth / host.RenderTargetHeight;   

                    // set new backbuffer
                    RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(GraphicsDevice, host.RenderTargetView, true);
                    GraphicsDevice.Presenter = new RenderTargetGraphicsPresenter(GraphicsDevice, backbufferRenderTarget);
                    GraphicsDevice.SetRenderTargets(backbufferRenderTarget);

                    terrainShader.Effect.Parameters["ScreenAspectRatio"].SetValue((float)host.RenderTargetWidth / host.RenderTargetHeight);
                }
            }
        }

        void WPFHost.IScene.Render()
        {
            lock (this)
            {
                if (DeactivateRendering)
                    return;
            }

            GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
            
            // setup camera
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.Effect.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["TerrainHeightmapSampler"].SetResource(linearBorderSamplerState);

            GraphicsDevice.SetVertexInputLayout(null);
            GraphicsDevice.SetVertexBuffer(0, (Buffer<Vector3>)null);

            // render screenspace terrain!
            terrainShader.Effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.PointList, 1);

            // render spheres
            GraphicsDevice.SetVertexInputLayout(sphereVertexInputLayout);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.NonPremultiplied);
            sphereBillboardShader.Effect.Parameters["CameraPosition"].SetValue(camera.Position);
            sphereBillboardShader.Effect.Parameters["WorldViewProjection"].SetValue(viewProjection);
            foreach (PointSet pointSet in pointSets)
            {
                sphereBillboardShader.Effect.Parameters["Color"].SetValue(pointSet.Color.ToVector3());
                sphereBillboardShader.Effect.Parameters["InvertedRendering"].SetValue(pointSet.InvertedRendering);
                sphereBillboardShader.Effect.CurrentTechnique.Passes[0].Apply();
                pointSet.Draw(GraphicsDevice);
            }
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Opaque);

            DoConemapTask();
        }

        void WPFHost.IScene.OnResize(WPFHost.ISceneHost host)
        {
            if (GraphicsDevice == null)
                return;

            lock (this)
            {
                resizeNeeded = true;
            }
        }

        public void OnLeftMouseDown()
        {
            Ray ray = camera.GetPickingRay(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            pointSetWithSelection = null;
            foreach (PointSet pointSet in pointSets)
            {
                if (pointSet.SelectSphere(ray, terrainScale, terrainTranslation))
                    pointSetWithSelection = pointSet;
            }
        }

        public void OnLeftMouseUp()
        {
            pointSetWithSelection = null;
        }

        public void OnMouseMove()
        {
            Ray ray = camera.GetPickingRay(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            if (pointSetWithSelection != null)
                pointSetWithSelection.MoveSelectedSphere(ref ray, terrainScale, terrainTranslation);
        }

        internal void OnMouseWheel(int delta)
        {
            if (pointSetWithSelection != null)
                pointSetWithSelection.MoveSelectedSphere(delta);
        }
    }
}
