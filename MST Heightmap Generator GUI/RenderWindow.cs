using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Diagnostics;
using System.Threading;

namespace MST_Heightmap_Generator_GUI
{
    public class RenderWindow : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;

        private Effect terrainShader;

        private bool terrainReady = false;
        private int width, height;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public RenderWindow()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "";

            graphicsDeviceManager.PreferredBackBufferWidth = 1024;
            graphicsDeviceManager.PreferredBackBufferHeight = 768;
            Window.AllowUserResizing = false;
            
        }

        protected override void Initialize()
        {
            Window.Title = "HelloWorld!";

            EffectCompilerFlags compilerFlags = EffectCompilerFlags.None;
#if DEBUG
            compilerFlags |= EffectCompilerFlags.Debug;
#endif
            /*var terrainShaderCompileResult = EffectCompiler.CompileFromFile("terrain.fx", compilerFlags);
            if (terrainShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(terrainShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }
            terrainShader = new Effect(GraphicsDevice, terrainShaderCompileResult.EffectData);
            */
            base.Initialize();
        }

        public void Refresh(int width, int height)
        {
            // todo mutex/semaphore
            this.width = width;
            this.height= height;
            terrainReady = true;
        }

        private void RenderTerrain()
        {
            // todo mutex/semaphore
            terrainShader.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.PointList, width * height);

            Thread.EndCriticalRegion();
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.Black);

            if (terrainReady)
                RenderTerrain();

            base.Draw(gameTime);
        }
    }
}
