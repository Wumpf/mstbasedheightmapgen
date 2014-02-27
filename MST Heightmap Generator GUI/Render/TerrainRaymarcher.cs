using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MST_Heightmap_Generator_GUI.Render
{
    class TerrainRaymarcher : Terrain
    {
        private float heightScale;
        public float HeightScale
        {
            get
            {
                return heightScale;
            } 
            set
            {
                heightScale = value;

                if (terrainShader != null)
                {
                    var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
                    heightmapConstantBuffer.Parameters["TerrainScale"].SetValue(heightScale);
                    heightmapConstantBuffer.IsDirty = true;
                }
            } 
        }

        private Vector3 lightDirection;
        public Vector3 LightDirection
        {
            get
            {
                return lightDirection;
            }
            set
            {
                if (terrainShader != null)
                {
                    lightDirection = value;
                    System.Diagnostics.Debug.Assert(terrainShader.Effect.Parameters["LightDirection"] != null, "Terrain Shader does not contain the constant \"LightDirection\"");
                    terrainShader.Effect.Parameters["LightDirection"].SetValue(lightDirection);
                }
            }
        }

        private float screenAspectRatio = 1.0f;
        public float ScreenAspectRatio
        {
            get
            {
                return screenAspectRatio;
            }
            set
            {
                if(terrainShader != null)
                {
                    screenAspectRatio = value;
                    System.Diagnostics.Debug.Assert(terrainShader.Effect.Parameters["ScreenAspectRatio"] != null, "Terrain Shader does not contain the constant \"ScreenAspectRatio\"");
                    terrainShader.Effect.Parameters["ScreenAspectRatio"].SetValue(value);
                }
            }
        }

        public Vector3 Translation { get; set; }

        private Texture heightmapTexture;
        private float heightmapPixelPerWorldUnit = 1;
        private uint numHeightmapMipLevels = 0;

        private ShaderAutoReload terrainShader;
#if CONEMAPPING_RAYMARCH
        private ShaderAutoReload computeRelaxedConeShader;

        Texture2D tempSingleChannelHeightmap;
        Texture2D tempSingleChannelCones;
        Stack<Vector2> conemapProcessingWorkItems = new Stack<Vector2>();
#else
        private ShaderAutoReload maxmapGenShader;
#endif
        private SamplerState linearBorderSamplerState;

        public TerrainRaymarcher(GraphicsDevice graphicsDevice, float aspectRatio)
        {
            var shaderMacros = new List<EffectData.ShaderMacro>();
    #if CONEMAPPING_RAYMARCH
                computeRelaxedConeShader = new ShaderAutoReload("shader/computerelaxedconemap.fx", GraphicsDevice);
                shaderMacros.Add(new EffectData.ShaderMacro("CONEMAPPING_RAYMARCH", "1"));
    #else
            maxmapGenShader = new ShaderAutoReload("shader/maxmapgen.fx", graphicsDevice);
    #endif
            terrainShader = new ShaderAutoReload("shader/terrainRaymarch.fx", graphicsDevice, shaderMacros);
            terrainShader.Effect.Parameters["ScreenAspectRatio"].SetValue(aspectRatio   );
            terrainShader.OnReload += () => SetupTerrainConstants(graphicsDevice);
            
            // linear sampler
            var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
            samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Border;
            samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear;
            samplerStateDesc.BorderColor = Color4.Black;
            linearBorderSamplerState = SamplerState.New(graphicsDevice, "TerrainHeightmapSampler", samplerStateDesc);
        }

#if CONEMAPPING_RAYMARCH
        private void DoConemapTask()
        {
            if (conemapProcessingWorkItems.Count > 0)
            {
                // compute another fragment
                Vector2 textureAreaMin = conemapProcessingWorkItems.Pop();

                computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Compute"];
                
                computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(tempSingleChannelHeightmap);
                computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource(tempSingleChannelCones);
                computeRelaxedConeShader.Effect.Parameters["TextureAreaMin"].SetValue<int>(new int[] { (int)textureAreaMin.X, (int)textureAreaMin.Y });
                computeRelaxedConeShader.Effect.ConstantBuffers[0].Update();

                computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.Dispatch(tempSingleChannelHeightmap.Width / 32, tempSingleChannelHeightmap.Height / 32, 1);
                computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(false);

                // combine
                CombineTempMapsToCombined();


                // cleanup
                if (conemapProcessingWorkItems.Count == 0)
                {
                    tempSingleChannelHeightmap.Dispose();
                    tempSingleChannelCones.Dispose();
                }
            }
        }


        private void CombineTempMapsToCombined()
        {
            terrainShader.Effect.Parameters["Heightmap"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);

            computeRelaxedConeShader.Effect.CurrentTechnique = computeRelaxedConeShader.Effect.Techniques["Combine"];
            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource(tempSingleChannelHeightmap);
            computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource(tempSingleChannelCones);
            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource(heightmapTexture);

            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Dispatch(tempSingleChannelHeightmap.Width / 32, tempSingleChannelHeightmap.Height / 32, 1);
            computeRelaxedConeShader.Effect.CurrentTechnique.Passes[0].UnApply(false);
        }
#else
        private void GenerateMaxMap(RenderTarget2D texture, GraphicsDevice graphicsDevice)
        {
            // Save old render setup to restore later.
            SharpDX.Direct3D11.DepthStencilView depthStencilBefore;
            var renderTargetsBefore = graphicsDevice.GetRenderTargets(out depthStencilBefore);
            ViewportF oldViewport = graphicsDevice.GetViewport(0);

            numHeightmapMipLevels = 0;
            var currentWidth = texture.Width / 2;
            var currentHeight = texture.Height / 2;
            for (var mipLevel = 1; currentWidth > 0 && currentHeight > 0; ++mipLevel, currentWidth /= 2, currentHeight /= 2)
            {
                // Generate sampler on-the-fly.
                var samplerStateDesc = SharpDX.Direct3D11.SamplerStateDescription.Default();
                samplerStateDesc.AddressV = SharpDX.Direct3D11.TextureAddressMode.Clamp;
                samplerStateDesc.AddressU = SharpDX.Direct3D11.TextureAddressMode.Clamp;
                samplerStateDesc.Filter = SharpDX.Direct3D11.Filter.MinMagMipPoint;
                samplerStateDesc.MinimumLod = mipLevel-1;
                samplerStateDesc.MaximumLod = mipLevel-1;
                samplerStateDesc.MipLodBias = mipLevel-1;
                var mipLevelSamplerState = SamplerState.New(graphicsDevice, "MipLevelSampler_" + mipLevel, samplerStateDesc);

                // Draw.
                maxmapGenShader.Effect.Parameters["NearestSampler"].SetResource(mipLevelSamplerState);
                maxmapGenShader.Effect.Parameters["InputTexture"].SetResource(texture.ShaderResourceView[ViewType.Single, 0, mipLevel-1]);
                graphicsDevice.SetRenderTargets(texture.RenderTargetView[ViewType.Single, 0, mipLevel]);
                graphicsDevice.SetViewport(0, 0, currentWidth, currentHeight);
                maxmapGenShader.Effect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.Draw(PrimitiveType.PointList, 1);
                maxmapGenShader.Effect.CurrentTechnique.Passes[0].UnApply();

                ++numHeightmapMipLevels;
            }
            graphicsDevice.SetRenderTargets(depthStencilBefore, renderTargetsBefore);
            graphicsDevice.SetViewport(oldViewport);
        }
#endif

        public void LoadNewHeightMap(float[,] heightmap, float heightmapPixelPerWorldUnit, GraphicsDevice graphicsDevice)
        {
            this.heightmapPixelPerWorldUnit = heightmapPixelPerWorldUnit;

            // remove old textures
            terrainShader.Effect.Parameters["Heightmap"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            if (heightmapTexture != null)
                heightmapTexture.Dispose();
#if CONEMAPPING_RAYMARCH

            computeRelaxedConeShader.Effect.Parameters["HeightInput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            computeRelaxedConeShader.Effect.Parameters["ConesOutput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            computeRelaxedConeShader.Effect.Parameters["CombinedOutput"].SetResource<ShaderResourceViewSelector>((ShaderResourceViewSelector)null);
            if (tempSingleChannelHeightmap != null)
                tempSingleChannelHeightmap.Dispose();
            if (tempSingleChannelCones != null)
                tempSingleChannelCones.Dispose();

            // generate jobs
            conemapProcessingWorkItems.Clear();
            const int TILE_SIZE = 32;   // if you change this value, you also have to change " AreaPerCall in computerelaxedconemap.fx
            for (int x = 0; x < heightmap.GetLength(0); x += TILE_SIZE)
            {
                for (int y = 0; y < heightmap.GetLength(1); y += TILE_SIZE)
                    conemapProcessingWorkItems.Push(new Vector2(x, y));
            }
            

            // create temp-textures
            tempSingleChannelHeightmap = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource);
            tempSingleChannelHeightmap.SetData<float>(heightmap.Cast<float>().ToArray());
            tempSingleChannelCones = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R32.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            GraphicsDevice.Clear(tempSingleChannelCones, new Color4(2.0f));
  
            // compose to heightmap texture
            heightmapTexture = Texture2D.New(GraphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), 0, PixelFormat.R16G16.Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            CombineTempMapsToCombined();
#else
            // Create new heightmap.
            heightmapTexture = RenderTarget2D.New(graphicsDevice, heightmap.GetLength(0), heightmap.GetLength(1), MipMapCount.Auto, PixelFormat.R32.Float,
                                                                TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            unsafe
            {
                fixed (float* p = heightmap)
                {
                    heightmapTexture.SetData(new DataPointer(p, heightmap.GetLength(0) * heightmap.GetLength(1) * sizeof(float)), 0, 0);
                }
            }

            GenerateMaxMap((RenderTarget2D)heightmapTexture, graphicsDevice);
#endif

            Translation = new Vector3(-heightmapTexture.Width * 0.5f / heightmapPixelPerWorldUnit, 0, -heightmapTexture.Height * 0.5f / heightmapPixelPerWorldUnit);

            // setup heightmap cbuffer
            SetupTerrainConstants(graphicsDevice);
        }

        private void SetupTerrainConstants(GraphicsDevice graphicsDevice)
        {
            var heightmapConstantBuffer = terrainShader.Effect.ConstantBuffers["HeightmapInfo"];
            heightmapConstantBuffer.Parameters["HeightmapResolution"].SetValue(new Vector2(heightmapTexture.Width, heightmapTexture.Height));
            heightmapConstantBuffer.Parameters["HeightmapResolutionInv"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height));   // HeightmapResolutionInv
            heightmapConstantBuffer.Parameters["WorldUnitToHeightmapTexcoord"].SetValue(new Vector2(1.0f / heightmapTexture.Width, 1.0f / heightmapTexture.Height) * heightmapPixelPerWorldUnit);
            heightmapConstantBuffer.Parameters["HeightmapPixelSizeInWorld"].SetValue(new Vector2(1.0f / heightmapPixelPerWorldUnit));
            heightmapConstantBuffer.Parameters["NumHeightmapMipLevels"].SetValue(numHeightmapMipLevels);

            heightmapConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["TerrainHeightmapSampler"].SetResource(linearBorderSamplerState);
            terrainShader.Effect.Parameters["CubemapSampler"].SetResource(graphicsDevice.SamplerStates.LinearWrap);

            HeightScale = HeightScale;
            LightDirection = LightDirection;
            ScreenAspectRatio = ScreenAspectRatio;
        }

        public void Draw(GraphicsDevice graphicsDevice, Camera camera, Texture skyCubemap)
        {
            // setup camera
            Matrix viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix viewProjectionInverse = viewProjection; viewProjectionInverse.Invert();
            var cameraConstantBuffer = terrainShader.Effect.ConstantBuffers["Camera"];
            cameraConstantBuffer.Set(0, viewProjectionInverse);
            cameraConstantBuffer.Set(sizeof(float) * 4 * 4, camera.Position);
            cameraConstantBuffer.IsDirty = true;

            terrainShader.Effect.Parameters["Heightmap"].SetResource(heightmapTexture);
            terrainShader.Effect.Parameters["SkyCubemap"].SetResource(skyCubemap);
            terrainShader.Effect.Parameters["TerrainHeightmapSampler"].SetResource(linearBorderSamplerState);

            graphicsDevice.SetVertexInputLayout(null);
            graphicsDevice.SetVertexBuffer(0, (Buffer<Vector3>)null);

            // Render screen-space terrain, including sky!
            terrainShader.Effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.PointList, 1);
        }
    }
}
