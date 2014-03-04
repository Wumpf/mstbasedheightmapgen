using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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
        private GraphicsDevice graphicsDevice;
        private System.Windows.Threading.Dispatcher dispatcher;

        private FileSystemWatcher watcher;

        private List<EffectData.ShaderMacro> shaderMacros;

        public ShaderAutoReload(String shaderFilename, GraphicsDevice graphicsDevice, List<EffectData.ShaderMacro> shaderMacros = null, double checkEvery_ms = 500.0)
        {
            this.shaderFilename = Path.GetFullPath(shaderFilename);
            this.graphicsDevice = graphicsDevice;
            this.shaderMacros = shaderMacros;
            ReloadShader();

            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetFullPath(Path.GetDirectoryName(shaderFilename));
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += OnSourceFileChanged;
            watcher.Filter = "*.*"; // For unknown reason *.fx didn't work. The documentation says that it should be possible to set the specific file as filter http://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher%28VS.71%29.aspx

            watcher.EnableRaisingEvents = true;
        }

        private void OnSourceFileChanged(object source, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(shaderFilename)) // For unknown reason there are a lot of temp files that start with the full path and have some characters at the end of the actual filename.
            {
                try
                {
                    dispatcher.Invoke(ReloadShader);
                }
                catch(Exception) {    } // Embarrassingly I also have to admit that I have AGAIN no clue what is going on here. There are plenty of NullPtr exceptions but ignoring works fine. >.<
            }
        }

        private void ReloadShader()
        {
            if(Effect != null)
                Effect.Dispose();

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
