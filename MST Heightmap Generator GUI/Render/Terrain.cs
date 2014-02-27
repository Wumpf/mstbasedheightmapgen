using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MST_Heightmap_Generator_GUI.Render
{
  interface Terrain
  {
      float HeightScale { set; }
      Vector3 LightDirection { set; }
      float ScreenAspectRatio { set; }

      Vector3 Translation { get; }

      void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit, GraphicsDevice graphicsDevice);
      void Draw(GraphicsDevice graphicsDevice, Camera camera, Texture skyCubemap);
  }
}
