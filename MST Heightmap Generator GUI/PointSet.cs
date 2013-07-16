using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace MST_Heightmap_Generator_GUI
{
    using SharpDX.Toolkit.Graphics;

    /// <summary>
    /// representing a renderable and editable set of points on the terrain
    /// </summary>
    public class PointSet
    {
        public bool Visible { get; set; }

        private Buffer<Vector3> vertexBuffer;
        private Vector3[] spherePositionArray;

        private Vector3 pointRenderingTranslationOffset;

        private int selectedSphere = -1;

        private const float SPHERE_RADIUS = 2.3f;
        private const float UPDOWN_MOVESPEED = 0.0001f;
        private static readonly Vector3 POS_MAX = new Vector3(2048, 1, 2048);
        private static readonly Vector3 POS_MIN = new Vector3(-2048, 0, -2048);

        public PointSet(float[,] points, float heightmapPixelPerWorldUnit, float heightmapTextureWidth, float heightmapTextureHeight, GraphicsDevice graphicsDevice) : 
            this(points, new Vector3(-heightmapTextureWidth, 0, -heightmapTextureHeight) * 0.5f / heightmapPixelPerWorldUnit, graphicsDevice)
        {
        }

        public PointSet(float[,] points, Vector3 pointRenderingTranslationOffset, GraphicsDevice graphicsDevice)
        {
            this.pointRenderingTranslationOffset = pointRenderingTranslationOffset;

            // setup spherepositions
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            spherePositionArray = new Vector3[points.GetLength(0)];
            for (int i = 0; i < spherePositionArray.Length; ++i)
                spherePositionArray[i] = new Vector3(points[i, 0], points[i, 2], points[i, 1]) + pointRenderingTranslationOffset;
            vertexBuffer = Buffer.Vertex.New<Vector3>(graphicsDevice, spherePositionArray, SharpDX.Direct3D11.ResourceUsage.Dynamic);

            Visible = true;
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            if (Visible)
            {
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                graphicsDevice.Draw(PrimitiveType.PointList, vertexBuffer.ElementCount);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if any sphere was selected</returns>
        public bool SelectSphere(Ray pickingRay, float terrainScale)
        {
            for (int i = 0; i < spherePositionArray.Length; ++i)
            {
                Vector3 pos = spherePositionArray[i];
                pos.Y *= terrainScale;
                if (new BoundingSphere(pos, SPHERE_RADIUS).Intersects(ref pickingRay))
                {
                    selectedSphere = i;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// moves the currently selected sphere
        /// </summary>
        /// <param name="moveVec"></param>
        public void MoveSelectedSphere(ref Ray pickingRay, float terrainScale)
        {
            if (selectedSphere >= 0)
            {
                Vector3 intersect;
                bool hit = false;
                // vertical
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
                    hit = new Plane(Vector3.UnitX, -spherePositionArray[selectedSphere].X).Intersects(ref pickingRay, out intersect);
                // horizontal
                else
                    hit = new Plane(Vector3.UnitY, -spherePositionArray[selectedSphere].Y * terrainScale).Intersects(ref pickingRay, out intersect);

                if (hit)
                {
                    spherePositionArray[selectedSphere] = intersect;
                    spherePositionArray[selectedSphere].X = MathUtil.Clamp(spherePositionArray[selectedSphere].X, POS_MIN.X, POS_MAX.X);
                    spherePositionArray[selectedSphere].Y = MathUtil.Clamp(spherePositionArray[selectedSphere].Y / terrainScale, POS_MIN.Y, POS_MAX.Y);
                    spherePositionArray[selectedSphere].Z = MathUtil.Clamp(spherePositionArray[selectedSphere].Z, POS_MIN.Z, POS_MAX.Z);
                    UpdateSpherePositionsBuffer();
                }
            }
        }
        public void MoveSelectedSphere(int mouseWheelDelta)
        {
            if (selectedSphere >= 0)
            {
                spherePositionArray[selectedSphere].Y = MathUtil.Clamp(spherePositionArray[selectedSphere].Y + mouseWheelDelta * UPDOWN_MOVESPEED, POS_MIN.Y, POS_MAX.Y);
                UpdateSpherePositionsBuffer();
            }
        }

        private void UpdateSpherePositionsBuffer()
        {
            unsafe
            {
                fixed (Vector3* pArray = spherePositionArray)
                {
                    IntPtr intPtr = new IntPtr((void*)pArray);
                    vertexBuffer.SetDynamicData(vertexBuffer.GraphicsDevice, (x) =>
                    {
                        Vector3* vecArray = (Vector3*)x;
                        for (int i = 0; i < spherePositionArray.Length; ++i)
                            vecArray[i] = spherePositionArray[i];
                    });
                }
            }
        }
    }
}
