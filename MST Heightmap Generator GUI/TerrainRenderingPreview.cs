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
        private FreeCamera camera;

        public bool Closing { get; set; }

        private GraphicsDevice graphicsDevice;
        private Texture2D heightmapTexture;

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
        }

        void WPFHost.IScene.Attach(WPFHost.ISceneHost host)
        {
            camera = new FreeCamera((float)host.RenderTargetWidth / host.RenderTargetHeight);

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

            // render screenspace terrain!
            terrainShader.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.PointList, 1);
        }
    }
}
