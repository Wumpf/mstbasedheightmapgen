﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using System;
using System.Diagnostics;
using System.Threading;

namespace MST_Heightmap_Generator_GUI
{
    public class TerrainRenderingPreview : WPFHost.IScene
    {
        private SharpDX.Toolkit.Graphics.Effect terrainShader;
        private FreeCamera camera;

        public bool Closing { get; set; }

        private GraphicsDevice graphicsDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public TerrainRenderingPreview()
        {
            camera = new FreeCamera(1.0f);
        }

        void WPFHost.IScene.Attach(WPFHost.ISceneHost host)
        {
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
            graphicsDevice.Draw(PrimitiveType.PointList, 1);
        }
    }
}
