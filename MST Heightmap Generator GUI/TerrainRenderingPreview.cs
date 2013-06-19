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
        private Effect terrainShader;
        private Effect computeRelaxedConeShader;

        private Camera camera;

        public bool Closing { get; set; }

        private WPFHost.ISceneHost host;
        private GraphicsDevice graphicsDevice;
        private Texture heightmapTexture;

        private SamplerState linearSamplerState;

        private bool resizeNeeded = false;


        #region Spheres
        
        private Buffer<Vector3> spherePositions;
        private VertexInputLayout sphereVertexInputLayout;

        private Effect sphereBillboardShader;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
        }

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit, float[,] summits)
        {
            if(heightmapTexture != null)
                heightmapTexture.Dispose();

            var heightOnlyTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource);
            heightOnlyTexture.SetData<float>(heightmap.Cast<float>().ToArray());
            heightmapTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R16G16.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            computeRelaxedConeShader.Parameters["HeightInput"].SetResource(heightOnlyTexture);
            computeRelaxedConeShader.Parameters["HeightWithConesOutput"].SetResource(heightmapTexture);
            //computeRelaxedConeShader.Parameters["LinearSampler"].SetResource(linearSamplerState);
            computeRelaxedConeShader.CurrentTechnique.Passes[0].Apply();
           
            graphicsDevice.Dispatch(heightmap.GetLength(0) / 32, heightmap.GetLength(1) / 32, 1);
           
            computeRelaxedConeShader.CurrentTechnique.Passes[0].UnApply(true);
            heightOnlyTexture.Dispose();

            // setup camera
           // camera.Position = new Vector3(heightmapTexture.Width/2, 100.0f, heightmapTexture.Height / 2);

            // setup heightmap cbuffer
            var heightmapConstantBuffer = terrainShader.ConstantBuffers["HeightmapInfo"];
            const float terrainScale = 50.0f;
            heightmapConstantBuffer.Parameters["TerrainScale"].SetValue(terrainScale);
            heightmapConstantBuffer.Parameters["HeightmapSize"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            heightmapConstantBuffer.Parameters["HeightmapSizeInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapSizeInv
            heightmapConstantBuffer.Parameters["WorldUnitToHeightmapTexcoord"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height) * heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.Parameters["HeightmapPixelSizeInWorld"].SetValue(1.0f / heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.IsDirty = true;

            terrainShader.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Parameters["LinearSampler"].SetResource(linearSamplerState);

            // setup spherepositions
            if (spherePositions != null)
                spherePositions.Dispose();

            Vector3[] spherePositionArray = new Vector3[summits.GetLength(0)];
            for (int i = 0; i < spherePositionArray.Length; ++i)
                spherePositionArray[i] = new Vector3(summits[i, 1], summits[i, 2] * terrainScale, summits[i, 0]) - new Vector3(heightmapTexture.Width, 0, heightmapTexture.Height) * 0.5f / heightmapPixelPerWorldUnit;
            spherePositions = Buffer.Vertex.New<Vector3>(graphicsDevice, spherePositionArray, SharpDX.Direct3D11.ResourceUsage.Dynamic);
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
            
  

            // load terrain shader
            EffectCompilerFlags compilerFlags = EffectCompilerFlags.None;
#if DEBUG
            compilerFlags |= EffectCompilerFlags.Debug;
#endif
            var shaderCompiler = new EffectCompiler();
                      
            // relaxed cone mapping compute shader
            var computeRelaxedConeShaderCompileResult = shaderCompiler.CompileFromFile("computerelaxedconemap.fx", compilerFlags);
            if (computeRelaxedConeShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(computeRelaxedConeShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }
            computeRelaxedConeShader = new Effect(graphicsDevice, computeRelaxedConeShaderCompileResult.EffectData);
            
            // load terrain shader
            var terrainShaderCompileResult = shaderCompiler.CompileFromFile("terrain.fx", compilerFlags);
            if (terrainShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(terrainShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }
            terrainShader = new Effect(graphicsDevice, terrainShaderCompileResult.EffectData);
            terrainShader.ConstantBuffers["Camera"].Parameters["ScreenAspectRatio"].SetValue((float)host.RenderTargetWidth / host.RenderTargetHeight);



            var sphereBillboardShaderCompileResult = shaderCompiler.CompileFromFile("spherebillboards.fx", compilerFlags);
            if (sphereBillboardShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(sphereBillboardShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }
            sphereBillboardShader = new Effect(graphicsDevice, sphereBillboardShaderCompileResult.EffectData);
            


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

                    terrainShader.Parameters["ScreenAspectRatio"].SetValue((float)host.RenderTargetWidth / host.RenderTargetHeight);
                }
            }
        }

        void WPFHost.IScene.Render()
        {
            graphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
            
            // setup camera
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;

            terrainShader.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Parameters["LinearSampler"].SetResource(linearSamplerState);

            graphicsDevice.SetVertexInputLayout(null);
            graphicsDevice.SetVertexBuffer(0, (Buffer<Vector3>)null);

            // render screenspace terrain!
            terrainShader.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.PointList, 1);

            // render spheres
            if (spherePositions != null)
            {
                sphereBillboardShader.Parameters["CameraRight"].SetValue(camera.Right);
                sphereBillboardShader.Parameters["CameraUp"].SetValue(Vector3.Cross(camera.Right, camera.Direction));
                sphereBillboardShader.Parameters["WorldViewProjection"].SetValue(viewProjection);

                graphicsDevice.SetBlendState(graphicsDevice.BlendStates.NonPremultiplied);
                graphicsDevice.SetVertexBuffer(spherePositions);
                graphicsDevice.SetVertexInputLayout(sphereVertexInputLayout);
                sphereBillboardShader.CurrentTechnique.Passes[0].Apply();
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
    }
}
