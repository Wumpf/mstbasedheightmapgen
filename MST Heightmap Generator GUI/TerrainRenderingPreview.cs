using SharpDX;

using System;
using System.Linq;
using System.Collections.Generic;

namespace MST_Heightmap_Generator_GUI
{
    using SharpDX.Toolkit.Graphics;
    

    public class TerrainRenderingPreview : WPFHost.IScene
    {
        private WPFHost.ISceneHost host;
        public GraphicsDevice GraphicsDevice { get; private set; }

        private ShaderAutoReload terrainShader;
        private ShaderAutoReload computeRelaxedConeShader;

        private VertexInputLayout sphereVertexInputLayout;
        private ShaderAutoReload sphereBillboardShader;

        private List<PointSet> pointSets = new List<PointSet>();
        PointSet containingSelectedSphere = null;

        private Camera camera;
        
        #region heightmap properties

        private Texture heightmapTexture;

        private float heightmapPixelPerWorldUnit;
        private float minHeight;
        private float maxHeight;

        private float terrainScale;

        #endregion

        private SamplerState linearSamplerState;

        


        public bool Closing { get; set; }
        private bool resizeNeeded = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
        }

        #region PointSet Functions
        public void ClearPointSet()
        {
            pointSets.Clear();
        }
        public void AddPointSet(PointSet newPointSet)
        {
            pointSets.Add(newPointSet);
        }
        public void RemovePointSet(PointSet pointSet)
        {
            pointSets.Remove(pointSet);
        }
        #endregion


        /// <summary>
        /// The visual output can be rescaled without regeneration of the map.
        /// 
        /// This method should always be called together with LoadNewHeightMap.
        /// </summary>
        /// <param name="minHeight">Measured minimum terrain height times a scaling factor</param>
        /// <param name="maxHeight">Measured maximum terrain height times a scaling factor</param>
        public void RescaleHeight(float minHeight, float maxHeight)
        {
            this.maxHeight = maxHeight;
            this.minHeight = minHeight;

            var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
            terrainScale = maxHeight - minHeight;
            heightmapConstantBuffer.Parameters["TerrainScale"].SetValue(terrainScale);
            heightmapConstantBuffer.Parameters["MaxTerrainHeight"].SetValue(maxHeight);
            heightmapConstantBuffer.Parameters["MinTerrainHeight"].SetValue(minHeight);
            heightmapConstantBuffer.IsDirty = true;

            sphereBillboardShader.Effect.Parameters["HeightScale"].SetValue(terrainScale);
        }

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit)
        {
            this.heightmapPixelPerWorldUnit = heightmapPixelPerWorldUnit;

            if(heightmapTexture != null)
                heightmapTexture.Dispose();

            // compute
            var tempHeights = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource);
            tempHeights.SetData<float>(heightmap.Cast<float>().ToArray());
            var tempCones = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            GraphicsDevice.Clear(tempCones, new Color4(2.0f));

            computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Compute"];
            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(tempHeights);
            computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource(tempCones);
            //computeRelaxedConeShader.Effect.Parameters["LinearSampler"].SetResource(linearSamplerState);

            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();
            const int TILE_SIZE = 32;   // if you change this value, you also have to change " AreaPerCall in computerelaxedconemap.fx
            for (int x = 0; x < heightmap.GetLength(0); x += TILE_SIZE)
            {
                for (int y = 0; y < heightmap.GetLength(1); y += TILE_SIZE)
                {
                    computeRelaxedConeShader.Effect.Parameters["TextureAreaMin"].SetValue<int>(new int[]{x,y});
                    computeRelaxedConeShader.Effect.ConstantBuffers[0].Update();
                    GraphicsDevice.Dispatch(heightmap.GetLength(0) / 32, heightmap.GetLength(1) / 32, 1);
                    //GraphicsDevice.Flush();
                }
            }
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(true);
            
            // compose to heightmap texture
            heightmapTexture = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R16G16.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Combine"];
            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource(heightmapTexture);
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();

            GraphicsDevice.Dispatch(heightmap.GetLength(0) / 32, heightmap.GetLength(1) / 32, 1);
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(true);

            // clean
            tempHeights.Dispose();
            tempCones.Dispose();

            // setup camera
           // camera.Position = new Vector3(heightmapTexture.Width/2, 100.0f, heightmapTexture.Height / 2);

            // setup heightmap cbuffer
            SetupHeightmapConstants();
        }

        private void SetupHeightmapConstants()
        {
            var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
            heightmapConstantBuffer.Parameters["HeightmapSize"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            heightmapConstantBuffer.Parameters["HeightmapSizeInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapSizeInv
            heightmapConstantBuffer.Parameters["WorldUnitToHeightmapTexcoord"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height) * heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.Parameters["HeightmapPixelSizeInWorld"].SetValue(new Vector2(1.0f / heightmapPixelPerWorldUnit));
            heightmapConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["LinearSampler"].SetResource(linearSamplerState);
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
            terrainShader.OnReload += SetupHeightmapConstants;
            terrainShader.OnReload += () => RescaleHeight(minHeight, maxHeight);

            // linear sampler
            var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
            samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear;
            samplerStateDesc.BorderColor = Color4.Black;
            linearSamplerState = SamplerState.New(GraphicsDevice, "LinearSampler", samplerStateDesc);

            // dummy heightmap
            heightmapTexture = Texture2D.New(GraphicsDevice, 1, 1, MipMapCount.Auto, PixelFormat.R32.Float);
            heightmapTexture.SetData<float>(new float[] { 0.0f });

            // vertex input layout
            sphereVertexInputLayout = VertexInputLayout.New(VertexBufferLayout.New(0, VertexElement.Position(SharpDX.DXGI.Format.R32G32B32_Float)));
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
                    // todo: new camera sucks
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
            GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
            
            // setup camera
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.Effect.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["LinearSampler"].SetResource(linearSamplerState);

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
            sphereBillboardShader.Effect.CurrentTechnique.Passes[0].Apply();
            foreach (PointSet pointSet in pointSets)
                pointSet.Draw(GraphicsDevice);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Opaque);
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
            containingSelectedSphere = null;
            foreach (PointSet pointSet in pointSets)
            {
                if(pointSet.SelectSphere(ray, terrainScale))
                    containingSelectedSphere = pointSet;
            }
        }

        public void OnLeftMouseUp()
        {
            containingSelectedSphere = null;
        }

        public void OnMouseMove()
        {
            Ray ray = camera.GetPickingRay(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            if (containingSelectedSphere != null)
                containingSelectedSphere.MoveSelectedSphere(ref ray, terrainScale);
        }

        internal void OnMouseWheel(int delta)
        {
            if (containingSelectedSphere != null)
                containingSelectedSphere.MoveSelectedSphere(delta);
        }
    }
}
