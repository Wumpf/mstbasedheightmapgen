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

        private FreeCamera camera;

        public bool Closing { get; set; }

        private bool terrainReady = false;
        private int width, height;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloWorldGame" /> class.
        /// </summary>
        public RenderWindow()
        {
            IsMouseVisible = true;

            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "";

            graphicsDeviceManager.PreferredBackBufferWidth = 1024;
            graphicsDeviceManager.PreferredBackBufferHeight = 768;
            Window.AllowUserResizing = false;

            camera = new FreeCamera((float)graphicsDeviceManager.PreferredBackBufferWidth / graphicsDeviceManager.PreferredBackBufferHeight);
        }

        protected override void Initialize()
        {
            Window.Title = "HelloWorld!";

            EffectCompilerFlags compilerFlags = EffectCompilerFlags.None;
#if DEBUG
            compilerFlags |= EffectCompilerFlags.Debug;
#endif
            var terrainShaderCompileResult = EffectCompiler.CompileFromFile("terrain.fx", compilerFlags);
            if (terrainShaderCompileResult.HasErrors)
            {
                System.Console.WriteLine(terrainShaderCompileResult.Logger.Messages);
                Debugger.Break();
            }
            terrainShader = new Effect(GraphicsDevice, terrainShaderCompileResult.EffectData);
            
            base.Initialize();
        }

        /// <summary>
        /// Sets new heightmap.
        /// This function is thread-safe!
        /// </summary>
        /// <param name="width">width of the new heightmap</param>
        /// <param name="height">height of the new heightmap</param>
        public void Refresh(int width, int height)
        {
            Thread.BeginCriticalRegion();

            this.width = width;
            this.height= height;
            terrainReady = true;

            Thread.EndCriticalRegion();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Closing)
                Exit();

            camera.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.Black);

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
            Thread.BeginCriticalRegion();
            if (terrainReady)
            {
                terrainShader.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.Draw(PrimitiveType.PointList, 1);
            }
            Thread.EndCriticalRegion();

            base.Draw(gameTime);
        }
    }
}
