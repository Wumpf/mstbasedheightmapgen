using SharpDX;
using SharpDX.D3DCompiler;

using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace MST_Heightmap_Generator_GUI
{
    using SharpDX.Toolkit.Graphics;

    public class TerrainRenderingPreview : WPFHost.IScene
    {
        private ShaderAutoReload terrainShader;
        private ShaderAutoReload computeRelaxedConeShader;


        private Camera camera;

        public bool Closing { get; set; }

        private WPFHost.ISceneHost host;
        private GraphicsDevice graphicsDevice;
        
        private Texture heightmapTexture;

        #region heightmap properties
        private float heightmapPixelPerWorldUnit;
        private float minHeight;
        private float maxHeight;
        #endregion

        private SamplerState linearSamplerState;

        private float terrainScale;

        private bool resizeNeeded = false;


        #region Spheres
        
        private const float SPHERE_RADIUS = 2.3f;

        private Buffer<Vector3> spherePositions;
        private Vector3[] spherePositionArray;
        private VertexInputLayout sphereVertexInputLayout;

        private ShaderAutoReload sphereBillboardShader;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
        }

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

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit, float[,] summits)
        {
            this.heightmapPixelPerWorldUnit = heightmapPixelPerWorldUnit;

            if(heightmapTexture != null)
                heightmapTexture.Dispose();

            var heightOnlyTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource);
            heightOnlyTexture.SetData<float>(heightmap.Cast<float>().ToArray());
            heightmapTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R16G16.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(heightOnlyTexture);
            computeRelaxedConeShader.Effect.Parameters["HeightWithConesOutput"].SetResource(heightmapTexture);
            //computeRelaxedConeShader.Effect.Parameters["LinearSampler"].SetResource(linearSamplerState);
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();
           
            graphicsDevice.Dispatch(heightmap.GetLength(0) / 32, heightmap.GetLength(1) / 32, 1);
           
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(true);
            heightOnlyTexture.Dispose();

            // setup camera
           // camera.Position = new Vector3(heightmapTexture.Width/2, 100.0f, heightmapTexture.Height / 2);

            // setup heightmap cbuffer
            SetupHeightmapConstants();

            // setup spherepositions
            if (spherePositions != null)
                spherePositions.Dispose();

            spherePositionArray = new Vector3[summits.GetLength(0)];
            for (int i = 0; i < spherePositionArray.Length; ++i)
                spherePositionArray[i] = new Vector3(summits[i, 0], summits[i, 2], summits[i, 1]) - new Vector3(heightmapTexture.Width, 0, heightmapTexture.Height) * 0.5f / heightmapPixelPerWorldUnit;
            spherePositions = Buffer.Vertex.New<Vector3>(graphicsDevice, spherePositionArray, SharpDX.Direct3D11.ResourceUsage.Dynamic);
        }

        void SetupHeightmapConstants()
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
            graphicsDevice = GraphicsDevice.New(host.Device);
            RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(graphicsDevice, host.RenderTargetView, true);
            graphicsDevice.Presenter = new RenderTargetGraphicsPresenter(graphicsDevice, backbufferRenderTarget);
            graphicsDevice.SetRenderTargets(backbufferRenderTarget);
            
  

            // load shader
            computeRelaxedConeShader = new ShaderAutoReload("computerelaxedconemap.fx", graphicsDevice);
            terrainShader = new ShaderAutoReload("terrain.fx", graphicsDevice);
            sphereBillboardShader = new ShaderAutoReload("spherebillboards.fx", graphicsDevice);
            terrainShader.OnReload += SetupHeightmapConstants;
            terrainShader.OnReload += () => RescaleHeight(minHeight, maxHeight);

            // linear sampler
            var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
            samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear;
            samplerStateDesc.BorderColor = Color4.Black;
            linearSamplerState = SamplerState.New(graphicsDevice, "LinearSampler", samplerStateDesc);

            // dummy heightmap
            heightmapTexture = Texture2D.New(graphicsDevice, 1, 1, MipMapCount.Auto, PixelFormat.R32.Float);
            heightmapTexture.SetData<float>(new float[] { 0.0f });

            // vertex input layout
            sphereVertexInputLayout = VertexInputLayout.New(VertexBufferLayout.New(0, VertexElement.Position(SharpDX.DXGI.Format.R32G32B32_Float)));
        }

        void WPFHost.IScene.Detach()
        {
            graphicsDevice.Dispose();
            graphicsDevice = null;
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
                    RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(graphicsDevice, host.RenderTargetView, true);
                    graphicsDevice.Presenter = new RenderTargetGraphicsPresenter(graphicsDevice, backbufferRenderTarget);
                    graphicsDevice.SetRenderTargets(backbufferRenderTarget);

                    terrainShader.Effect.Parameters["ScreenAspectRatio"].SetValue((float)host.RenderTargetWidth / host.RenderTargetHeight);
                }
            }
        }

        void WPFHost.IScene.Render()
        {
            graphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
            
            // setup camera
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.Effect.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["LinearSampler"].SetResource(linearSamplerState);

            graphicsDevice.SetVertexInputLayout(null);
            graphicsDevice.SetVertexBuffer(0, (Buffer<Vector3>)null);

            // render screenspace terrain!
            terrainShader.Effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.PointList, 1);

            // render spheres
            if (spherePositions != null)
            {
 //               sphereBillboardShader.Parameters["CameraRight"].SetValue(camera.Right);
//                sphereBillboardShader.Parameters["CameraUp"].SetValue(Vector3.Cross(camera.Right, camera.Direction));
                sphereBillboardShader.Effect.Parameters["CameraPosition"].SetValue(camera.Position);
                sphereBillboardShader.Effect.Parameters["WorldViewProjection"].SetValue(viewProjection);

                graphicsDevice.SetBlendState(graphicsDevice.BlendStates.NonPremultiplied);
                graphicsDevice.SetVertexBuffer(spherePositions);
                graphicsDevice.SetVertexInputLayout(sphereVertexInputLayout);
                sphereBillboardShader.Effect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.Draw(PrimitiveType.PointList, spherePositions.ElementCount);

                graphicsDevice.SetBlendState(graphicsDevice.BlendStates.Opaque);
            }
        }

        void WPFHost.IScene.OnResize(WPFHost.ISceneHost host)
        {
            if (graphicsDevice == null)
                return;

            lock (this)
            {
                resizeNeeded = true;
            }
        }

        int selectedSphere = -1;

        public void OnLeftMouseDown()
        {
            if (spherePositionArray == null)
                return;

            Ray ray = camera.GetPickingRay(graphicsDevice.BackBuffer.Width, graphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            for (int i = 0; i < spherePositionArray.Length; ++i)
            {
                Vector3 pos = spherePositionArray[i];
                pos.Y *= terrainScale;
                if (new BoundingSphere(pos, SPHERE_RADIUS).Intersects(ref ray))
                    selectedSphere = i;
            }
        }

        public void OnLeftMouseUp()
        {
            selectedSphere = -1;
        }


        private System.Windows.Point lastMousePosition;

        public void OnMouseMove()
        {
            System.Windows.Point currentMousePosition = System.Windows.Input.Mouse.GetPosition(host.WindowsInputElement);
            if (selectedSphere >= 0)
            {
                spherePositionArray[selectedSphere].Y += (float)(lastMousePosition.Y - currentMousePosition.Y) / terrainScale;
                UpdateSpherePositionsBuffer();
            }

            lastMousePosition = currentMousePosition;
        }

        private void UpdateSpherePositionsBuffer()
        {
            unsafe
            {
                fixed (Vector3* pArray = spherePositionArray)
                {
                    IntPtr intPtr = new IntPtr((void*)pArray);
                    spherePositions.SetDynamicData(graphicsDevice, (x) =>
                    {
                        Vector3* vecArray = (Vector3*)x;
                        for (int i = 0; i < spherePositionArray.Length; ++i)
                            vecArray[i] = spherePositionArray[i];
                    });
                }
            }
        }
    }
}
