using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Toolkit.Graphics;

namespace MST_Heightmap_Generator_GUI.Render
{
    public class RenderPreviewPanel : WPFHost.IScene
    {
        private bool deactivateRendering = false;
        public bool DeactivateRendering
        { get { return deactivateRendering; } set { deactivateRendering = value; } }

        public GraphicsDevice GraphicsDevice { get; private set; }

        #region intern rendering data

        private WPFHost.ISceneHost host;
        
        private Terrain terrain;
        
        private ShaderAutoReload skyShader;
        
        private VertexInputLayout sphereVertexInputLayout;
        private ShaderAutoReload sphereBillboardShader;

        #endregion

        private List<PointSet> pointSets = new List<PointSet>();
        private PointSet pointSetWithSelection = null;

        private Camera camera;
        
        private float terrainScale = 20;

        #region Sky

        public float TimeOfDay
        {
            get
            {
                return timeOfDay;
            }
            set
            {
                timeOfDay = value;
                if (terrain != null)
                    terrain.LightDirection = LightDirection;
                ReGenerateSkyCubeMap();
            }
        }
        private float timeOfDay = 0.3f;

        public Vector3 LightDirection
        {
            get
            {
                return new Vector3((float)Math.Cos(Math.PI * timeOfDay), (float)Math.Sin(Math.PI * timeOfDay), 0);
            }
        }

        const int CUBEMAP_RES = 512;
        private RenderTargetCube skyCubemap;

        #endregion
       
        public bool Closing { get; set; }
        private bool resizeNeeded = false;

        public RenderPreviewPanel()
        {
            DeactivateRendering = false;
        }

        #region PointSet Functions

        /// <summary>
        /// Removes all PointSet.
        /// </summary>
        public void ClearPointSet()
        {
            pointSets.Clear();
        }
        /// <summary>
        /// Adds a PointSet.
        /// </summary>
        /// <param name="newPointSet">New point set</param>
        public void AddPointSet(PointSet newPointSet)
        {
            newPointSet.InitGraphicsRessource(GraphicsDevice);
            pointSets.Add(newPointSet);
        }
        /// <summary>
        /// Removes a PointSet.
        /// </summary>
        /// <param name="pointSet">PointSet to remove</param>
        public void RemovePointSet(PointSet pointSet)
        {
            pointSets.Remove(pointSet);
        }

        #endregion


        /// <summary>
        /// The visual output can be rescaled without regeneration of the map.
        /// </summary>
        /// <param name="terrainScale">Scale factor</param>
        public void SetScaleFactor(float terrainScale)
        {
            this.terrainScale = terrainScale;

            if (terrain != null)
                terrain.HeightScale = terrainScale;
            
            if(sphereBillboardShader != null)
                sphereBillboardShader.Effect.Parameters["HeightScale"].SetValue(terrainScale);
        }

        private void ReGenerateSkyCubeMap()
        {
            if (GraphicsDevice == null)
                return;

            SharpDX.Direct3D11.DepthStencilView depthStencilBefore;
            var renderTargetsBefore = GraphicsDevice.GetRenderTargets(out depthStencilBefore);

            skyShader.Effect.Parameters["LightDirection"].SetValue(LightDirection);
            GraphicsDevice.SetRenderTargets(skyCubemap.RenderTargetView[ViewType.Full, 0, 0]);
            skyShader.Effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.PointList, 1);

            GraphicsDevice.SetRenderTargets(depthStencilBefore, renderTargetsBefore);
        }

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit)
        {
            terrain.LoadNewHeightMap(heightmap, heightmapPixelPerWorldUnit, GraphicsDevice);
        }

        private void SetupConstants()
        {
            sphereBillboardShader.Effect.Parameters["Translation"].SetValue(terrain.Translation);
        }

        void WPFHost.IScene.Attach(WPFHost.ISceneHost host)
        {
            this.host = host;
            camera = new Camera((float)host.RenderTargetWidth / host.RenderTargetHeight, (float)(75.0 * Math.PI / 180.0), 0.1f, 10000.0f);

            // device setup
            if (host.Device == null)
                throw new Exception("Scene host device is null");
            GraphicsDevice = GraphicsDevice.New(host.Device);
            RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(GraphicsDevice, host.RenderTargetView, true);
            GraphicsDevice.Presenter = new RenderTargetGraphicsPresenter(GraphicsDevice, backbufferRenderTarget);
            GraphicsDevice.SetRenderTargets(backbufferRenderTarget);
            
 
            terrain = new TerrainRaymarcher(GraphicsDevice, (float)host.RenderTargetWidth / host.RenderTargetHeight);
            terrain.HeightScale = terrainScale;

            // load shader
            sphereBillboardShader = new ShaderAutoReload("shader/spherebillboards.fx", GraphicsDevice);

            // vertex input layout
            sphereVertexInputLayout = VertexInputLayout.New(VertexBufferLayout.New(0, VertexElement.Position(SharpDX.DXGI.Format.R32G32B32_Float)));

            // generate sky
            skyShader = new ShaderAutoReload("shader/sky.fx", GraphicsDevice);
            skyShader.OnReload += ReGenerateSkyCubeMap;
            skyCubemap = RenderTargetCube.New(GraphicsDevice, CUBEMAP_RES, 0, PixelFormat.R8G8B8A8.SNorm);
            TimeOfDay = timeOfDay;

            // constant buffer to defaults
            SetupConstants();
        }

        void WPFHost.IScene.Detach()
        {
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
        }

        void WPFHost.IScene.Update(TimeSpan sceneTime)
        {
          camera.Update((float)sceneTime.TotalSeconds);

          lock (this)
          {
              if (resizeNeeded)
              {
                  camera.AspectRatio = (float)host.RenderTargetWidth / host.RenderTargetHeight;   

                  // set new backbuffer
                  RenderTarget2D backbufferRenderTarget = RenderTarget2D.New(GraphicsDevice, host.RenderTargetView, true);
                  GraphicsDevice.Presenter = new RenderTargetGraphicsPresenter(GraphicsDevice, backbufferRenderTarget);
                  GraphicsDevice.SetRenderTargets(backbufferRenderTarget);

                  terrain.ScreenAspectRatio = camera.AspectRatio;
                  
                  resizeNeeded = false;
              }
          }
        }

        void WPFHost.IScene.Render()
        {
            lock (this)
            {
                if (DeactivateRendering)
                    return;
            }

            GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
            
            terrain.Draw(GraphicsDevice, camera, skyCubemap);
            
            // render spheres
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            GraphicsDevice.SetVertexInputLayout(sphereVertexInputLayout);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.NonPremultiplied);
            sphereBillboardShader.Effect.Parameters["CameraPosition"].SetValue(camera.Position);
            sphereBillboardShader.Effect.Parameters["WorldViewProjection"].SetValue(viewProjection);
            sphereBillboardShader.Effect.Parameters["Translation"].SetValue(terrain.Translation);
            foreach (PointSet pointSet in pointSets)
            {
                sphereBillboardShader.Effect.Parameters["Color"].SetValue(pointSet.Color.ToVector3());
                sphereBillboardShader.Effect.Parameters["InvertedRendering"].SetValue(pointSet.InvertedRendering);
                sphereBillboardShader.Effect.CurrentTechnique.Passes[0].Apply();
                pointSet.Draw(GraphicsDevice);
            }
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Opaque);

#if CONEMAPPING_RAYMARCH
            DoConemapTask();
#endif
        }

        void WPFHost.IScene.OnResize(WPFHost.ISceneHost host)
        {
            if (GraphicsDevice == null)
                return;

            lock (this)
            {
                resizeNeeded = true;
            }
        }

        public void OnLeftMouseDown()
        {
            Ray ray = camera.GetPickingRay(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            pointSetWithSelection = null;
            foreach (PointSet pointSet in pointSets)
            {
                if (pointSet.SelectSphere(ray, terrainScale, terrain.Translation))
                    pointSetWithSelection = pointSet;
            }
        }

        public void OnLeftMouseUp()
        {
            pointSetWithSelection = null;
        }

        public void OnMouseMove()
        {
            Ray ray = camera.GetPickingRay(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, host.WindowsInputElement);
            if (pointSetWithSelection != null)
                pointSetWithSelection.MoveSelectedSphere(ref ray, terrainScale, terrain.Translation);
        }

        internal void OnMouseWheel(int delta)
        {
            if (pointSetWithSelection != null)
                pointSetWithSelection.MoveSelectedSphere(delta);
        }
    }
}
