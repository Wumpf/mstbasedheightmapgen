using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Toolkit.Graphics;
using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace MST_Heightmap_Generator_GUI
{
    public class TerrainRenderingPreview : WPFHost.IScene
    {
        private SharpDX.Toolkit.Graphics.Effect terrainShader;
        private Camera camera;

        public bool Closing { get; set; }

        private WPFHost.ISceneHost host;
        private GraphicsDevice graphicsDevice;
        private Texture2D heightmapTexture;

        private SamplerState linearSamplerState;

        private bool resizeNeeded = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
        }

        public void LoadNewHeightMap(float[,] heightmap)
        {
            if(heightmapTexture != null)
                heightmapTexture.Dispose();

            heightmapTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), MipMapCount.Auto, PixelFormat.R32.Float);
            heightmapTexture.SetData<float>(heightmap.Cast<float>().ToArray());

            // setup camera
           // camera.Position = new Vector3(heightmapTexture.Width/2, 100.0f, heightmapTexture.Height / 2);

            // setup heightmap cbuffer
            /* var heightmapConstantBuffer = terrainShader.ConstantBuffers["Heightmap"];
             heightmapConstantBuffer.Set(0, new Vector2(heightmapTexture.Width, heightmapTexture.Height));   // HeightmapSize
             heightmapConstantBuffer.Set(sizeof(float) * 2, 1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapSizeInv
             heightmapConstantBuffer.IsDirty = true;*/
            terrainShader.Parameters["HeightmapSize"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            terrainShader.Parameters["HeightmapSizeInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));

            terrainShader.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Parameters["LinearSampler"].SetResource(linearSamplerState);
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
            var terrainShaderCompileResult = shaderCompiler.CompileFromFile("terrain.fx", compilerFlags);
            if (terrainShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(terrainShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }

            terrainShader = new SharpDX.Toolkit.Graphics.Effect(graphicsDevice, terrainShaderCompileResult.EffectData);

            // linear sampler
            var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
            samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagLinearMipPoint;   // no mipmapping
            samplerStateDesc.BorderColor = Color4.Black;
            linearSamplerState = SamplerState.New(graphicsDevice, "LinearSampler", samplerStateDesc);

            // dummy heightmap
            heightmapTexture = Texture2D.New(graphicsDevice, 1, 1, MipMapCount.Auto, PixelFormat.R32.Float);
            heightmapTexture.SetData<float>(new float[] { 0.0f });    
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

            // render screenspace terrain!
            terrainShader.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.PointList, 1);
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
