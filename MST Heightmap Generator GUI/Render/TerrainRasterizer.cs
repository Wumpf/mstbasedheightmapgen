using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Toolkit.Graphics;

namespace MST_Heightmap_Generator_GUI.Render
{
    class TerrainRasterizer : Terrain
    {
        #region Interface Getter/Setter

        float heightScale = 1.0f;
        public float HeightScale
        {
            get { return heightScale; }
            set
            {
                heightScale = value;
            
                var heightmapConstantBuffer = shader.Effect.ConstantBuffers["HeightmapInfo"];
                heightmapConstantBuffer.Parameters["TerrainScale"].SetValue(heightScale);
                heightmapConstantBuffer.IsDirty = true;
            }
        }

        Vector3 lightDirection;
        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set
            {
                lightDirection = value;

                lightDirection = value;
                System.Diagnostics.Debug.Assert(shader.Effect.Parameters["LightDirection"] != null, "Terrain Shader does not contain the constant \"LightDirection\"");
                shader.Effect.Parameters["LightDirection"].SetValue(lightDirection);
            }
        }

        float screenAspectRatio;
        public float ScreenAspectRatio
        {
            set { screenAspectRatio = value; ; }
        }

        Vector3 translation;
        public Vector3 Translation
        {
            private set { translation = value; }
            get { return translation; }
        }

        #endregion

        class InstancedGeomClipMapping
        {
            private float minPatchSizeWorld;

            private readonly uint ringThinkness;
            private readonly uint numRings;

            // Contains immutable relative patch positions.
            private SharpDX.Toolkit.Graphics.Buffer<Half2> patchVertexBuffer;

            private VertexInputLayout vertexInputLayout;

            private enum PatchType
            {
                FULL,
                STITCH1,
                STITCH2
            };

            struct PatchInstanceData
            {
                public float worldPositionX;
                public float worldPositionY;
                public float worldScale;
                public uint rotationType;
            };

            private SharpDX.Toolkit.Graphics.Buffer<ushort>[] patchIndexBuffer = new SharpDX.Toolkit.Graphics.Buffer<ushort>[Enum.GetValues(typeof(PatchType)).Length];
            private SharpDX.Toolkit.Graphics.Buffer[] patchInstanceBuffer = new SharpDX.Toolkit.Graphics.Buffer[Enum.GetValues(typeof(PatchType)).Length];
            private uint[] maxPatchInstances = new uint[Enum.GetValues(typeof(PatchType)).Length];


            // Serves as CPU buffer for instance data. The element counter will be used to determine how many instances are active at the moment.
            List<PatchInstanceData>[] currentInstanceData = new List<PatchInstanceData>[Enum.GetValues(typeof(PatchType)).Length];

            public InstancedGeomClipMapping(GraphicsDevice graphicsDevice, float minPatchSizeWorld, uint ringThinkness, uint numRings)
            {
                this.ringThinkness = ringThinkness;
                this.numRings = numRings;
                this.minPatchSizeWorld = minPatchSizeWorld;

                // Patch vertex buffer
                Half2[] patchVertices = new Half2[9];
                for (int x = 0; x < 3; ++x)
                {
                    for (int y = 0; y < 3; ++y)
                    {
                        patchVertices[x + y * 3] = new Half2(x * 0.5f, y * 0.5f);
                    }
                }
                patchVertexBuffer = Buffer<Half2>.New(graphicsDevice, patchVertices, BufferFlags.VertexBuffer, SharpDX.Direct3D11.ResourceUsage.Immutable);


                // Instance buffers
                // Try to guess max number of patches:
                maxPatchInstances[(uint)PatchType.FULL] = ringThinkness * ringThinkness * 4 * numRings;
                maxPatchInstances[(uint)PatchType.STITCH1] = (uint)(maxPatchInstances[(uint)PatchType.FULL] * (float)(4 * 2 * ringThinkness) / (2 * ringThinkness * 2 * ringThinkness));
                maxPatchInstances[(uint)PatchType.STITCH2] = maxPatchInstances[(uint)PatchType.STITCH1] / 4;

                for(int i = 0; i < patchInstanceBuffer.Length; ++i)
                {
                    patchInstanceBuffer[i] = SharpDX.Toolkit.Graphics.Buffer.New(
                        graphicsDevice, (int)maxPatchInstances[i] * sizeof(float) * 4,  sizeof(float) * 4, BufferFlags.VertexBuffer, SharpDX.Direct3D11.ResourceUsage.Dynamic);
                    currentInstanceData[i] = new List<PatchInstanceData>();
                }

                // Input layout.
                vertexInputLayout = VertexInputLayout.New(
                    VertexBufferLayout.New(0, new VertexElement[]{ new VertexElement("RELPOS", 0, SharpDX.DXGI.Format.R16G16_Float, 0) }, 0),
                    VertexBufferLayout.New(1, new VertexElement[]{ new VertexElement("WORLDPOS", 0, SharpDX.DXGI.Format.R32G32_Float, 0),    // worldPosition
                                                                   new VertexElement("SCALE", 0, SharpDX.DXGI.Format.R32_Float, 8),       // worldScale
                                                                   new VertexElement("ROTATION", 0, SharpDX.DXGI.Format.R32_UInt, 12) }, 1));       // rotationType

                // Patch index buffer
                // Full patch
                ushort[] indicesFull = new ushort[]{ 0, 1, 4, 4, 1, 2, 0, 4, 3, 4, 2, 5, 3, 4, 6, 6, 4, 7, 7, 4, 8, 8, 4, 5 };  // optimize?
                patchIndexBuffer[(uint)PatchType.FULL] = Buffer<ushort>.New(graphicsDevice, indicesFull, BufferFlags.IndexBuffer, SharpDX.Direct3D11.ResourceUsage.Immutable);
                // First stitch: Only one triangle at bottom
                ushort[] indicesStitch1 = new ushort[]{ 0, 1, 4, 4, 1, 2, 0, 4, 3, 4, 2, 5, 3, 4, 6, 6, 4, 8, 8, 4, 5 };  // optimize?
                patchIndexBuffer[(uint)PatchType.STITCH1] = Buffer<ushort>.New(graphicsDevice, indicesStitch1, BufferFlags.IndexBuffer, SharpDX.Direct3D11.ResourceUsage.Immutable);
                // Second stitch: Only one triangle at bottom and right
                ushort[] indicesStitch2 = new ushort[]{ 0, 1, 4, 4, 1, 2, 0, 4, 3, 3, 4, 6, 6, 4, 8, 8, 4, 2 };  // optimize?
                patchIndexBuffer[(uint)PatchType.STITCH2] = Buffer<ushort>.New(graphicsDevice, indicesStitch2, BufferFlags.IndexBuffer, SharpDX.Direct3D11.ResourceUsage.Immutable);

            }

            public void UpdateInstanceData(Vector3 cameraPosition)
            {
                for(int i = 0; i < currentInstanceData.Length; ++i)
                    currentInstanceData[i].Clear();

                Vector2 minBefore = Vector2.Zero;
                Vector2 maxBefore = Vector2.Zero;

                PatchInstanceData currentPatch;
                currentPatch.worldScale = minPatchSizeWorld;

                for(uint ring = 0; ring < numRings; ++ring)
                {
                    // snap to next grid
                    Vector2 cameraBlockPosition = new Vector2((float)Math.Floor(cameraPosition.X / currentPatch.worldScale / 2) * currentPatch.worldScale * 2,
                                                              (float)Math.Floor(cameraPosition.Z / currentPatch.worldScale / 2) * currentPatch.worldScale * 2);
                    //Vector2 positionMin = cameraBlockPosition - new Vector2(currentPatch.worldScale * ringThinkness);
                    //Vector2 positionMax = cameraBlockPosition + new Vector2(currentPatch.worldScale * ringThinkness);

                    Vector2 positionMax, positionMin;
                    positionMin.X = -MainWindow.MAP_SIZE / 2;
                    positionMin.Y = -MainWindow.MAP_SIZE / 2;
                    positionMax.X =  MainWindow.MAP_SIZE / 2;
                    positionMax.Y =  MainWindow.MAP_SIZE / 2;

                    for(currentPatch.worldPositionX = positionMin.X; currentPatch.worldPositionX < positionMax.X; currentPatch.worldPositionX += currentPatch.worldScale)
                    {
                        for(currentPatch.worldPositionY = positionMin.Y; currentPatch.worldPositionY < positionMax.Y; currentPatch.worldPositionY += currentPatch.worldScale)
                        {
                            // Skip tile position if it is within last ring. Since size doubles every time, these are not many.
                            if(!(currentPatch.worldPositionX < minBefore.X || currentPatch.worldPositionY < minBefore.Y ||
                                currentPatch.worldPositionX >= maxBefore.X || currentPatch.worldPositionY >= maxBefore.Y))
                            {
                                continue;
                            }

                            int xBorder = 0;
                            int yBorder = 0;
                            if(currentPatch.worldPositionY == positionMin.Y)
                              yBorder = -1;
                            else if(currentPatch.worldPositionY + currentPatch.worldScale >= positionMax.Y)
                              yBorder = 1;
                            if(currentPatch.worldPositionX == positionMin.X)
                              xBorder = -1;
                            else if(currentPatch.worldPositionX + currentPatch.worldScale >= positionMax.X)
                              xBorder = 1;

                            if(yBorder == -1)
                              currentPatch.rotationType = (uint)(xBorder == 1 ? 4 : 1);
                            else if(xBorder == -1)
                              currentPatch.rotationType = 3;
                            else if(yBorder == 1)
                              currentPatch.rotationType = 0;
                            else// if(xBorder == 1)
                              currentPatch.rotationType = 2;

                            if(xBorder == 0 && yBorder == 0)
                              currentInstanceData[(uint)PatchType.FULL].Add(currentPatch);
                            else
                            {
                              if(xBorder == 0 || yBorder == 0)
                                currentInstanceData[(uint)PatchType.STITCH1].Add(currentPatch);
                              else
                                currentInstanceData[(uint)PatchType.STITCH2].Add(currentPatch);
                            }
                        }
                    }

                    minBefore = positionMin;
                    maxBefore = positionMax;
                    currentPatch.worldScale *= 2;
                }

                // Upload to gpu.
                for (int i = 0; i < currentInstanceData.Length; ++i)
                {
                    System.Diagnostics.Debug.Assert(currentInstanceData[i].Count <= maxPatchInstances[i], "Too many patch instances!");
                    patchInstanceBuffer[i].SetData<PatchInstanceData>(currentInstanceData[i].ToArray(), 0, currentInstanceData[i].Count, 0, SetDataOptions.Discard);
                }
            }


            public void DrawGeometry(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.SetVertexBuffer(0, patchVertexBuffer, 4, 0);
                graphicsDevice.SetVertexInputLayout(vertexInputLayout);

                for(int i = 0; i < patchInstanceBuffer.Length; ++i)
                {
                    if (currentInstanceData[i].Count == 0)
                        continue;

                    graphicsDevice.SetIndexBuffer(patchIndexBuffer[i], false, 0);
                    graphicsDevice.SetVertexBuffer(1, patchInstanceBuffer[i], sizeof(float) * 4, 0);
                    graphicsDevice.DrawIndexedInstanced(PrimitiveType.PatchList(3), patchIndexBuffer[i].ElementCount, currentInstanceData[i].Count, 0, 0, 0);
                }
            }
        };

        private InstancedGeomClipMapping clipmapRenderer;
        private ShaderAutoReload shader;
        private Texture2D heightmapTexture;

        private float heightmapPixelPerWorldUnit = 1.0f;
        
        public TerrainRasterizer(GraphicsDevice graphicsDevice)
        {
            clipmapRenderer = new InstancedGeomClipMapping(graphicsDevice, 8.0f, 8, 4);
            shader = new ShaderAutoReload("shader/terrainRasterize.fx", graphicsDevice);
            shader.OnReload += () => SetupTerrainConstants(graphicsDevice);
        }


        private void SetupTerrainConstants(GraphicsDevice graphicsDevice)
        {
            var heightmapConstantBuffer = shader.Effect.ConstantBuffers["HeightmapInfo"];
            heightmapConstantBuffer.Parameters["HeightmapResolution"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            heightmapConstantBuffer.Parameters["HeightmapResolutionInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapResolutionInv
            heightmapConstantBuffer.Parameters["WorldUnitToHeightmapTexcoord"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height) * heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.Parameters["HeightmapPixelSizeInWorld"].SetValue(new Vector2(1.0f / heightmapPixelPerWorldUnit));

            heightmapConstantBuffer.IsDirty = true;

            shader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            shader.Effect.Parameters["TerrainHeightmapSampler"].SetResource(graphicsDevice.SamplerStates.LinearWrap);
       //     shader.Effect.Parameters["CubemapSampler"].SetResource(graphicsDevice.SamplerStates.LinearWrap);

            HeightScale = HeightScale;
            LightDirection = LightDirection;
        }

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit, SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice)
        {
            this.heightmapPixelPerWorldUnit = heightmapPixelPerWorldUnit;

            // remove old textures
            shader.Effect.Parameters["Heightmap"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            if (heightmapTexture != null)
                heightmapTexture.Dispose();

            // Create new heightmap.
            heightmapTexture = Texture2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), MipMapCount.Auto, PixelFormat.R32.Float);
            unsafe
            {
                fixed (float* p = heightmap)
                {
                    heightmapTexture.SetData(new DataPointer(p, heightmap.GetLength(0) * heightmap.GetLength(1) * sizeof(float)), 0, 0);
                }
            }
            // TODO? Mipmaps

            Translation = new Vector3(-heightmapTexture.Width * 0.5f / heightmapPixelPerWorldUnit, 0, -heightmapTexture.Height * 0.5f / heightmapPixelPerWorldUnit);

            // setup heightmap cbuffer
            SetupTerrainConstants(graphicsDevice);
        }

        public void Draw(SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice, Camera camera, SharpDX.Toolkit.Graphics.Texture skyCubemap)
        {
            if (heightmapTexture == null)
                return;

            clipmapRenderer.UpdateInstanceData(camera.Position);

            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = shader.Effect.ConstantBuffers["Camera"];
            cameraConstantBuffer.Parameters["View"].SetValue(camera.ViewMatrix);
            cameraConstantBuffer.Parameters["ViewProjection"].SetValue(viewProjection);
            cameraConstantBuffer.Parameters["InverseViewProjection"].SetValue(viewProjectionInverse);
            cameraConstantBuffer.Parameters["CameraPosition"].SetValue(camera.Position);
            cameraConstantBuffer.IsDirty = true;
            cameraConstantBuffer.Update();

            shader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
          //  shader.Effect.Parameters["SkyCubemap"].SetResource(skyCubemap);

        //    graphicsDevice.SetRasterizerState(graphicsDevice.RasterizerStates.WireFrameCullNone);
            graphicsDevice.SetDepthStencilState(graphicsDevice.DepthStencilStates.Default);

            shader.Effect.CurrentTechnique.Passes[0].Apply();
            clipmapRenderer.DrawGeometry(graphicsDevice);
            shader.Effect.CurrentTechnique.Passes[0].UnApply();
        }
    }
}