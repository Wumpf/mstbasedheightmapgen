using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Threading;

namespace MST_Heightmap_Generator_GUI
{
    public class TerrainRenderingPreview : WPFHost.IScene
    {
        private SharpDX.Direct3D11.Effect terrainShader;
        private FreeCamera camera;

        public bool Closing { get; set; }

        private Device graphicsDevice;
        private DeviceContext immediateContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
            camera = new FreeCamera(1.0f);
        }

        void WPFHost.IScene.Attach(WPFHost.ISceneHost host)
        {
            if (host.Device == null)
                throw new Exception("Scene host device is null");
            graphicsDevice = host.Device;
            immediateContext = host.Device.ImmediateContext;

            
            /*
            ShaderFlags compilerFlags = ShaderFlags.None;
#if DEBUG
            compilerFlags |= ShaderFlags.Debug;
#endif
            var terrainShaderCompileResult = ShaderBytecode.CompileFromFile("terrain.fx", "fx_5_0", compilerFlags);
            if (terrainShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(terrainShaderCompileResult.Message);
                Debugger.Break();
            }

            terrainShader = new SharpDX.Direct3D11.Effect(graphicsDevice, terrainShaderCompileResult.Bytecode); */
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
            // Clears the screen with the Color.CornflowerBlue
            
     //       immediateContext.ClearDepthStencilView(Color.Black);

       /*     // setup camera
            Matrix viewProjection = camera.ProjectionMatrix * camera.ViewMatrix;
            viewProjection.Transpose();
            Matrix viewProjectionInverse = viewProjection;
            viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;
            

            // render screenspace terrain!
            terrainShader.CurrentTechnique.Passes[0].Apply();
            immediateContext.Draw(PrimitiveType.PointList, 1);*/
        }
    }
}
