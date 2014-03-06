#include "screenalignedtriangle.fx"

//#define CONEMAPPING_RAYMARCH

cbuffer Camera : register(b0)
{
  float4x4 InverseViewProjection;
  float3 CameraPosition;
  float ScreenAspectRatio;
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

// ------------------------------------------------
// TERRAIN
// ------------------------------------------------
SamplerState TerrainHeightmapSampler;
Texture2D Heightmap;

float getTerrainHeight(in float2 worldPos)
{
  return Heightmap.SampleLevel(TerrainHeightmapSampler, worldPos*WorldUnitToHeightmapTexcoord + 0.5, 0).x * TerrainScale;
}

float3 getTerrainNormal(in float3 pos)
{
  float4 h;
  h[0] = getTerrainHeight(pos.xz + float2(0, -HeightmapPixelSizeInWorld.y));
  h[1] = getTerrainHeight(pos.xz + float2(0, HeightmapPixelSizeInWorld.y));
  h[2] = getTerrainHeight(pos.xz + float2(HeightmapPixelSizeInWorld.x, 0));
  h[3] = getTerrainHeight(pos.xz + float2(-HeightmapPixelSizeInWorld.x, 0));
  //float3 vecdz = float3(0.0f, h[1] - h[0], 2);
  //float3 vecdx = float3(2, h[2] - h[3], 0.0f);
  return normalize(float3(-h[2] + h[3], 2.0, -h[1] + h[0]));
}
// ------------------------------------------------
// RAYMARCH CORE
// ------------------------------------------------
static const float FarPlane = 1000;
static const float NearPlane = 3;

interface RaymarchMaxMapConfig
{
  int2 GetSamplingPosition(int2 samplingPos, int MipLevelSize);
};

#define XNEG 1
#define YNEG 2
#define ZNEG 4

// Function handles only X/Y positive for rayDirection. To perform conversion use correct functions in RaymarcMaxMapConfig
bool RaymarchMaxMap(in float3 texEntry, in float3 rayDirection_TextureSpace, out float3 worldIntersectionPoint, const int config)
{
  int numIterations = 0;
  int maxIterations = 500;

  // Currently examined miplevel, do not start at lowest level
  uint currentMipLevel = NumHeightmapMipLevels - 3;
  float currentLevelResolution = 8;

  float3 rayDirection_TexelSpace = rayDirection_TextureSpace;
  rayDirection_TexelSpace.xz *= currentLevelResolution;
  texEntry.xz *= currentLevelResolution;

  while (texEntry.y <= 1 && all(texEntry.xz <= currentLevelResolution) && all(texEntry >= 0))
  {
    ++numIterations;
    if (numIterations > maxIterations)
      break;

    // Compute tex exit
    float2 tXZ;// = (float2(1.0f, 1.0f) - frac(texEntry.xz)) / rayDirection_TexelSpace.xz;
    tXZ.x = (config & XNEG) ? frac(texEntry.x) : (1.0f - frac(texEntry.x));
    tXZ.y = (config & ZNEG) ? frac(texEntry.z) : (1.0f - frac(texEntry.z));
    tXZ /= abs(rayDirection_TexelSpace.xz);
    // The problem with negative stepping is that we have to express that we are staying closely *behind* the border - because of this there a epsilon subtracted
    float3 texExit;
    if(tXZ.x < tXZ.y)
      texExit = float3((config & XNEG) ? ceil(texEntry.x) - 1.00001f : (floor(texEntry.x) + 1.0f), texEntry.yz + rayDirection_TexelSpace.yz * tXZ.x);
    else
      texExit = float3(texEntry.xy + rayDirection_TexelSpace.xy * tXZ.y, (config & ZNEG) ? ceil(texEntry.z) - 1.00001f : floor(texEntry.z) + 1.0f);

    // Sample Height - some configurations need mirrored coordinates.
    float3 samplingPosition;
    samplingPosition.x = (config & XNEG) ? (texEntry.x) : (texEntry.x);
    samplingPosition.y = (config & ZNEG) ? (texEntry.z) : (texEntry.z);
    samplingPosition.z = currentMipLevel;
    float heightValue = Heightmap.Load((int3)samplingPosition).r;

    // Hit?
    float hitTest = (config & YNEG) ? texExit.y : texEntry.y;
    if (heightValue >= hitTest)
    {
      // Descending -> new hit!
      float3 intersectionPoint = texEntry;
      if (config & YNEG)
      {
        texExit = texEntry + max((heightValue - texEntry.y) / rayDirection_TexelSpace.y, 0) * rayDirection_TexelSpace;
        intersectionPoint = texExit;
      }

      if (currentMipLevel == 0)
      {
        // Todo: Need to interpolate on patch, or use at least two triangles
        // Todo: Gather4 would be better.
        // Todo: Possibly we should have done this on this miplevel first
        //float height01 = Heightmap.Load(int3(samplingPosition.x, ceil(samplingPosition.y), samplingPosition.z)).r;
        //float height10 = Heightmap.Load(int3(ceil(samplingPosition.x), samplingPosition.y, samplingPosition.z)).r;
        //float height11 = Heightmap.Load(int3(ceil(samplingPosition.x), ceil(samplingPosition.y), samplingPosition.z)).r;


        // bring intersection point to world and output
        worldIntersectionPoint.xz = intersectionPoint.xz * HeightmapResolutionInv;
        worldIntersectionPoint.xz -= 0.5f;
        worldIntersectionPoint.xz /= WorldUnitToHeightmapTexcoord;
        worldIntersectionPoint.y = intersectionPoint.y * TerrainScale;
        return true;
      }
      else
      {
        // Todo: Currently only going down, but could also go up in certain situations!

        --currentMipLevel;
        currentLevelResolution *= 2.0f;
        rayDirection_TexelSpace.xz *= 2.0f;
        texEntry = intersectionPoint;
        texEntry.xz *= 2.0f;
      }
    }
    else
      texEntry = texExit;
  }

  return false;
}

bool RayAABBTest(in float3 rayOrigin, in float3 rayDirection, in float3 boxMin, in float3 boxMax, out float t)
{
  float3 dirfrac = 1.0f / rayDirection;

  float3 t1 = (boxMin - rayOrigin) * dirfrac;
  float3 t2 = (boxMax - rayOrigin) * dirfrac;

  float tmin = max(max(min(t1.x, t2.x), min(t1.y, t2.y)), min(t1.z, t2.z));
  float tmax = min(min(max(t1.x, t2.x), max(t1.y, t2.y)), max(t1.z, t2.z));
 
  if (tmax <= 0 ||  // If tmax < 0, ray (line) is intersecting AABB, but whole AABB is behind us.
      tmin >= tmax) // If tmin > tmax, ray doesn't intersect AABB.
  {
      return false;
  }

  t = tmin;
  return true;
}

bool rayCast(in float3 rayOrigin, in float3 rayDirection, out float3 intersectionPoint, out float shadowTerm)
{
  float2 HalfWorldSize = HeightmapResolution * HeightmapPixelSizeInWorld * 0.5f;
  float3 WorldBoxMin = float3(-HalfWorldSize.x, 0, -HalfWorldSize.y);
  float3 WorldBoxMax = float3(HalfWorldSize.x, TerrainScale, HalfWorldSize.y);

  float rayOffset;

  // Inside the box?
  if(all(WorldBoxMin < rayOrigin) && all(WorldBoxMax > rayOrigin))
  {
    rayOffset = NearPlane;
  }
  else
  {
    // Hit the box?
    if(!RayAABBTest(rayOrigin, rayDirection, WorldBoxMin, WorldBoxMax, rayOffset))
      return false;
    rayOffset += 0.001f;
  }

  
  intersectionPoint = rayOrigin + rayOffset * rayDirection;


  // Ray direction in texture space - preserving the orginal unit length of rayDirection (meaning that it is NOT normalized in texture space)
  float3 rayDirection_TextureSpace;
  rayDirection_TextureSpace.xz = rayDirection.xz * WorldUnitToHeightmapTexcoord;
  rayDirection_TextureSpace.y = rayDirection.y / TerrainScale;

  // Transform to texture space
  intersectionPoint.xz = intersectionPoint.xz * WorldUnitToHeightmapTexcoord + 0.5f;
  intersectionPoint.y = intersectionPoint.y / TerrainScale;

  shadowTerm = 1.0f;

#ifdef CONEMAPPING_RAYMARCH
  static const float RayStepToHeightmapLod = 0.01f;
  static const float SoftShadowFactor = 200.0f; // lower means softer

  float dirXZlen = length(rayDirection_TextureSpace.xz);

  // cone stepping
  float lastShadowTerm = 1.0f;

  const int MaxNumConeSteps = 150;
  float minDistOffset = HeightmapResolutionInv.x / dirXZlen * 0.05;
  float distOffset;
  float deltaHeight;
  float travelled = 0.0;
  float softShadowingTravelFactor = SoftShadowFactor / dirXZlen;
  for (int i = 0; i < MaxNumConeSteps; ++i)
  {
    float2 height_cone = Heightmap.SampleLevel(TerrainHeightmapSampler, intersectionPoint.xz, 0).xy;
      deltaHeight = intersectionPoint.y - height_cone.x;

    // step cone
    distOffset = deltaHeight * height_cone.y / (dirXZlen - rayDirection_TextureSpace.y * height_cone.y);
    distOffset = max(minDistOffset, distOffset);
    intersectionPoint += rayDirection_TextureSpace * distOffset;

    // outside?
    if (any(intersectionPoint.xz != saturate(intersectionPoint.xz)) || intersectionPoint.y > 1)
      return false;

    // below terrain? done.
    if (deltaHeight < 0.0)
    {
      // rough estimate last point above
      intersectionPoint -= rayDirection_TextureSpace * (distOffset * 0.5f);

      // bring intersection point to world and output
      intersectionPoint.xz -= 0.5f;
      intersectionPoint.xz /= WorldUnitToHeightmapTexcoord;
      intersectionPoint.y = height_cone.x * TerrainScale;

      shadowTerm = 0.0f;

      return true;
    }

    travelled += distOffset;
    lastShadowTerm = shadowTerm;
    shadowTerm = lerp(min(shadowTerm, deltaHeight*SoftShadowFactor / travelled), lastShadowTerm, 0.8f);
  }
  // Left loop without terrain hit
  return false;
#else
  if (rayDirection_TextureSpace.y > 0)
  {
    if (rayDirection_TextureSpace.x > 0)
    {
      if (rayDirection_TextureSpace.z > 0)
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, 0);
      else
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, ZNEG);
    }
    else
    {
      if (rayDirection_TextureSpace.z > 0)
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, XNEG);
      else
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, XNEG | ZNEG);
    }
  }
  else
  {
    if (rayDirection_TextureSpace.x > 0)
    {
      if(rayDirection_TextureSpace.z > 0)
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, YNEG);
      else
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, YNEG | ZNEG);
    }
    else
    {
      if(rayDirection_TextureSpace.z > 0)
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, YNEG | XNEG);
      else
        return RaymarchMaxMap(intersectionPoint, rayDirection_TextureSpace, intersectionPoint, YNEG | XNEG | ZNEG);
    }
  }

  return false;
#endif

}

// heightmap display in upper right corner
static const float HeightmapCornerSize = 0.5f;
bool RenderHeightmapInCorner(float2 deviceCor, out float4 color)
{
    float2 heightmapArea = float2(HeightmapCornerSize, HeightmapCornerSize * ScreenAspectRatio);
    float2 onScreenHeightmapZero = float2(1.0f - heightmapArea.x, -1.0f);
    float2 heightmapCor = (float2(deviceCor.x, -deviceCor.y) - onScreenHeightmapZero) / heightmapArea;
    if (heightmapCor.x > 0 && heightmapCor.x < 1 && heightmapCor.y > 0 && heightmapCor.y < 1)
    {
      color = float4(Heightmap.SampleLevel(TerrainHeightmapSampler, heightmapCor, 0).rg, 0, 1);
      return true;
    }
    else
    {
      color = float4(1, 0, 1, 1);
      return false;
    }
}

float3 SampleSky(float3 direction)
{
  return SkyCubemap.SampleLevel(CubemapSampler, direction, 0).rgb;
}

static const float Ambient = 0.3f;
float4 PS(PS_INPUT input) : SV_Target
{
  float4 color;
  if (RenderHeightmapInCorner(input.DevicePos, color))
    return color;

  // "picking" - compute raydirection
  float2 deviceCor = input.DevicePos;
  float4 rayOrigin = mul(InverseViewProjection, float4(deviceCor, 0, 1));
  rayOrigin.xyz /= rayOrigin.w;
  float4 rayTarget = mul(InverseViewProjection, float4(deviceCor, 1, 1));
  rayTarget.xyz /= rayTarget.w;
  float3 rayDirection = normalize(rayTarget.xyz - rayOrigin.xyz);

  // Color
  float3 terrainPosition;
  float2 terrainDerivates;

  //int steps = 0;
  float3 outColor;
  float shadowTerm;
  if (rayCast(CameraPosition, rayDirection, terrainPosition, shadowTerm))
  {
    // LIGHTING
    float3 normal = getTerrainNormal(terrainPosition);
    float NDotL = dot(normal, LightDirection);
    float lighting = saturate(NDotL);
    float3 backLightDir = LightDirection;
    backLightDir.xz = -backLightDir.xz;


    float3 shadowCastPos;
    rayCast(terrainPosition, LightDirection, shadowCastPos, shadowTerm);
    lighting *= saturate(shadowTerm);
    lighting += saturate(dot(normal, backLightDir))*0.4f;

    outColor = 0.8*float3(0.9, 0.9, 0.88) * lighting;//SampleSky(normal)*0.4;

    // FOGGING
    // clever fog http://www.iquilezles.org/www/articles/fog/fog.htm
    float dist = length(terrainPosition - rayOrigin.xyz);
    float fogAmount = min(1, 0.5 * exp(-CameraPosition.y  * 0.01) * (1.0 - exp(-dist*rayDirection.y* 0.011)) / rayDirection.y);
    float3 fogColor = float3(0.18867780436772762, 0.4978442963618773, 0.6616065586417131)*0.9; // air color
    outColor = lerp(outColor, fogColor, fogAmount);
  }
  else
  {
    outColor = SampleSky(rayDirection);
  }

  return float4(outColor, 1.0f);
}

technique10 Render
{
  pass P0
  {
    Profile = 11.0;
    VertexShader = VS;
    HullShader = null;
    DomainShader = null;
    GeometryShader = GS;
    PixelShader = PS;
  }
}