using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MST_Heightmap_Generator_GUI
{
    class ShaderAutoReload
    {
        public Effect Effect { get; private set; }

        public event Action OnReload;

        private string shaderFilename;
        private DateTime lastShaderLoadTime;
        private GraphicsDevice graphicsDevice;
        private Timer checkReloadTimer;
        private System.Windows.Threading.Dispatcher dispatcher;

        private List<EffectData.ShaderMacro> shaderMacros;

        public ShaderAutoReload(String shaderFilename, GraphicsDevice graphicsDevice, List<EffectData.ShaderMacro> shaderMacros = null, double checkEvery_ms = 500.0)
        {
            this.shaderFilename = shaderFilename;
            this.graphicsDevice = graphicsDevice;
            this.shaderMacros = shaderMacros;
            ReloadShader();

            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            checkReloadTimer = new Timer(checkEvery_ms);
            checkReloadTimer.Elapsed += (a, b) => ReloadIfNecessary();
            checkReloadTimer.Enabled = true;
        }

        public void ReloadIfNecessary()
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(shaderFilename);
            if (fileInfo.LastWriteTimeUtc > lastShaderLoadTime)
                dispatcher.Invoke(ReloadShader);
        }

        private void ReloadShader()
        {
            if(Effect != null)
                Effect.Dispose();

            lastShaderLoadTime = DateTime.UtcNow;

            EffectCompilerFlags compilerFlags = EffectCompilerFlags.None;
#if DEBUG
            compilerFlags |= EffectCompilerFlags.Debug;
#endif
            var shaderCompiler = new EffectCompiler();

            // relaxed cone mapping compute shader
            var compileResult = shaderCompiler.CompileFromFile(shaderFilename, compilerFlags, shaderMacros);
            if (compileResult.HasErrors)
            {
                System.Console.WriteLine(compileResult.Logger.Messages);
                System.Diagnostics.Debugger.Break();
            }
            Effect = new Effect(graphicsDevice, compileResult.EffectData);

            if (OnReload != null)
                OnReload();
        }
    }
}
