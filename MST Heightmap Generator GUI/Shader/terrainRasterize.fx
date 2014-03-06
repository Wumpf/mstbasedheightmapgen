cbuffer Camera : register(b0)
{
  float4x4 InverseViewProjection;
  float4x4 View;
  float4x4 ViewProjection;
  float3 CameraPosition;
}

cbuffer HeightmapInfo : register(b2)
{
  float2 HeightmapResolution;
  float2 HeightmapResolutionInv;
  float2 WorldUnitToHeightmapTexcoord;
  float2 HeightmapPixelSizeInWorld;
  float TerrainScale;
  unsigned int NumHeightmapMipLevels;
}

float3 LightDirection;
SamplerState CubemapSampler;
TextureCube SkyCubemap;

static const float TrianglesPerClipSpaceUnit = 10.0f;
static const float MaxTesselationFactor = 32.0f;

SamplerState TerrainHeightmapSampler;
Texture2D Heightmap;

float getTerrainHeight(in float2 heightmapPos, float mipLevel = 0.0f)
{
  return Heightmap.SampleLevel(TerrainHeightmapSampler, heightmapPos, mipLevel).x * TerrainScale;
}

float3 getTerrainNormal(in float2 heightmapPos)
{
  float4 h;
  h[0] = getTerrainHeight(heightmapPos + float2(0, -HeightmapResolutionInv.y));
  h[1] = getTerrainHeight(heightmapPos + float2(0, HeightmapResolutionInv.y));
  h[2] = getTerrainHeight(heightmapPos + float2(HeightmapResolutionInv.x, 0));
  h[3] = getTerrainHeight(heightmapPos + float2(-HeightmapResolutionInv.x, 0));
  //float3 vecdz = float3(0.0f, h[1] - h[0], 2);
  //float3 vecdx = float3(2, h[2] - h[3], 0.0f);
  return normalize(float3(-h[2] + h[3], HeightmapPixelSizeInWorld.x * 2, -h[1] + h[0]));
}

// ---------------------------------------------------------------------------------------------------------------------------------------------
// Vertex Shader
// ---------------------------------------------------------------------------------------------------------------------------------------------

struct VS_IN
{
  float2 PatchRelPosition : RELPOS;
  float2 PatchWorldPosition : WORLDPOS;
  float PatchWorldScale : SCALE;
  uint PatchRotationType : ROTATION;
};

struct VS_OUT
{
  float3 WorldPos : WORLDPOS;
  float2 HeightmapCoord : TEXCOORD;
};

VS_OUT VS(VS_IN In)
{
  VS_OUT Out;

  float2 patchRelPosition = In.PatchRelPosition;

    // Real rotations are needed - do not break the vertex order, otherwise culling will fail.
  if(In.PatchRotationType % 2 != 0)
  {
    // Rotate 180°
    patchRelPosition = float2(1, 1) - patchRelPosition;
  }
  if(In.PatchRotationType > 1)
  {
    // Rotate 90°
    patchRelPosition -= float2(0.5, 0.5);
    patchRelPosition = float2(patchRelPosition.y, -patchRelPosition.x);
    patchRelPosition += float2(0.5, 0.5);
  }

  Out.WorldPos.xz = patchRelPosition.xy * In.PatchWorldScale + In.PatchWorldPosition;
  Out.HeightmapCoord = Out.WorldPos.xz * WorldUnitToHeightmapTexcoord + 0.5;

  // Todo: Using lower mipmaps could both improve quality and performance!
  float mipLevel = 0.0f;
  Out.WorldPos.y = Heightmap.SampleLevel(TerrainHeightmapSampler, Out.HeightmapCoord, mipLevel).x * TerrainScale;
  
  return Out;
}

// ---------------------------------------------------------------------------------------------------------------------------------------------
// Hull Shader
// ---------------------------------------------------------------------------------------------------------------------------------------------
struct HS_CONSTANT_DATA_OUTPUT
{
  float EdgeTessFactor[3]  	: SV_TessFactor;
  float InsideTessFactor : SV_InsideTessFactor;
};

// Estimates the size of a sphere around a given world space edge
float EstimateSphereSizeAroundEdge(float3 p0, float3 p1)
{
  float diameter = distance(p0, p1);
  float3 edgeMid = (p1 + p0) * 0.5;

    float3 camXRadius = diameter * float3(View[0].x, View[1].x, View[2].x);
    float2 clip0 = mul(float4(edgeMid, 1.0), ViewProjection).xw;
    float2 clip1 = mul(float4(edgeMid + camXRadius, 1.0), ViewProjection).xw;

    return abs(clip0.x / clip0.y - clip1.x / clip1.y);
};

struct HS_OUT
{
  float2 WorldPos2D : WORLDPOS2D;
  float2 HeightmapCoord : TEXCOORD;
};

[domain("tri")]
[partitioning("fractional_even")]
[outputtopology("triangle_ccw")]
[outputcontrolpoints(3)]
[patchconstantfunc("HS_PatchConstant")]
HS_OUT HS(InputPatch<VS_OUT, 3> In, uint i : SV_OutputControlPointID, uint PatchID : SV_PrimitiveID)
{
  HS_OUT Out;
  Out.WorldPos2D = In[i].WorldPos.xz;
  Out.HeightmapCoord = In[i].HeightmapCoord;
  return Out;
}

HS_CONSTANT_DATA_OUTPUT HS_PatchConstant(InputPatch<VS_OUT, 3> In, uint PatchID : SV_PrimitiveID)
{
  HS_CONSTANT_DATA_OUTPUT Out;

  float3 mid = 0.25 * (In[0].WorldPos + In[1].WorldPos + In[2].WorldPos);

  [unroll] for(int i = 0; i < 3; ++i)
  {
    const uint second = (i + 1) % 3;
    Out.EdgeTessFactor[(i + 2) % 3] = clamp(EstimateSphereSizeAroundEdge(In[i].WorldPos, In[second].WorldPos[second]) * TrianglesPerClipSpaceUnit, 2.0, MaxTesselationFactor);
  }

  Out.InsideTessFactor = (Out.EdgeTessFactor[0] + Out.EdgeTessFactor[1] + Out.EdgeTessFactor[2]) * 0.25;

  return Out;
}


// ---------------------------------------------------------------------------------------------------------------------------------------------
// Domain Shader
// ---------------------------------------------------------------------------------------------------------------------------------------------
struct DS_OUT
{
  float4 Position : SV_Position;
  float3 WorldPos : WORLDPOS;
  float2 HeightmapCoord : TEXCOORD;
};

[domain("tri")]
DS_OUT DS(HS_CONSTANT_DATA_OUTPUT Const, float3 DomainCoord : SV_DomainLocation, const OutputPatch<HS_OUT, 3> Verts)
{
  DS_OUT Out;
  Out.HeightmapCoord = DomainCoord.x * Verts[0].HeightmapCoord +
                       DomainCoord.y * Verts[1].HeightmapCoord +
                       DomainCoord.z * Verts[2].HeightmapCoord;

  Out.WorldPos.xz = DomainCoord.x * Verts[0].WorldPos2D +
                    DomainCoord.y * Verts[1].WorldPos2D +
                    DomainCoord.z * Verts[2].WorldPos2D;

  // Todo: Using lower mipmaps could both improve quality and performance!
  float mipLevel = 0.0f;
  Out.WorldPos.y = Heightmap.SampleLevel(TerrainHeightmapSampler, Out.HeightmapCoord, mipLevel).x * TerrainScale;

  Out.Position = mul(float4(Out.WorldPos, 1.0), ViewProjection);

  return Out;
}

// ---------------------------------------------------------------------------------------------------------------------------------------------
// Pixel Shader
// ---------------------------------------------------------------------------------------------------------------------------------------------
float4 PS(DS_OUT In) : SV_Target
{
  float3 outColor = float3(1, 0, 0);

  float3 normal = getTerrainNormal(In.HeightmapCoord);

  float3 cameraDirection = In.WorldPos - CameraPosition;
  float cameraDistance = length(cameraDirection);
  cameraDirection /= cameraDistance;

  float NDotL = dot(normal, LightDirection);
  float lighting = saturate(NDotL);
  float3 backLightDir = LightDirection;
  backLightDir.xz = -backLightDir.xz;

  lighting += saturate(dot(normal, backLightDir))*0.4f;

  outColor = 0.8*float3(0.9, 0.9, 0.88) * lighting;//SampleSky(normal)*0.4;

  // FOGGING
  // clever fog http://www.iquilezles.org/www/articles/fog/fog.htm
  float fogAmount = saturate(0.5 * exp(-CameraPosition.y  * 0.01) * (1.0 - exp(-cameraDistance * cameraDirection.y * 0.011)) / cameraDirection.y);
  float3 fogColor = float3(0.18867780436772762, 0.4978442963618773, 0.6616065586417131)*0.9; // air color
  outColor = lerp(outColor, fogColor, fogAmount);

  return float4(outColor, 1);
}

technique Render
{
  pass P0
  {
    Profile = 11.0;
    VertexShader = VS;
    HullShader = HS;
    DomainShader = DS;
    GeometryShader = null;
    PixelShader = PS;
  }
}