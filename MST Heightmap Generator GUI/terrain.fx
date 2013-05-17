#include "vertexprocessing.fx"
#include "sky.fx"

cbuffer Camera : register(b0)
{
	float4x4 InverseViewProjection; 
	float3 CameraPosition;
	float ScreenAspectRatio;
}

cbuffer HeightmapInfo : register(b2)
{
	float2 HeightmapSize;
	float2 HeightmapSizeInv;
	float2 WorldUnitToHeightmapTexcoord;
	float2 HeightmapPixelSizeInWorld;
}

// ------------------------------------------------
// TERRAIN
// ------------------------------------------------
static const float terrainScale = 40;
static const float maxTerrainHeight = terrainScale;
static const float minTerrainHeight = -10;

SamplerState LinearSampler;  
Texture2D Heightmap;  

float getTerrainHeight(in float2 pos, in float lod = 0.0f)
{
	return Heightmap.SampleLevel(LinearSampler, pos*WorldUnitToHeightmapTexcoord + 0.5, lod).x * terrainScale;
}

float3 getTerrainNormal(in float3 pos)
{
	float4 h;
	h[0] = getTerrainHeight(pos.xz + float2( 0,-HeightmapPixelSizeInWorld.y ));
	h[1] = getTerrainHeight(pos.xz + float2( 0, HeightmapPixelSizeInWorld.y ));
	h[2] = getTerrainHeight(pos.xz + float2( HeightmapPixelSizeInWorld.x, 0 ));
	h[3] = getTerrainHeight(pos.xz + float2(-HeightmapPixelSizeInWorld.x, 0 ));
	float3 vecdz = float3(0.0f, h[1] - h[0], 2);
	float3 vecdx = float3(2, h[2] - h[3], 0.0f);
    return normalize(cross(vecdz, vecdx));
}

// ------------------------------------------------
// RAYMARCH CORE
// ------------------------------------------------
static const float FarPlane = 1000;
static const float NearPlane = 3;
static const float RayStepToHeightmapLod = 0.01f;
bool rayCast(in float3 rayOrigin, in float3 rayDirection, out float3 intersectionPoint, out float dist, in float lodFactor = 1.0f)
{
	if(rayDirection.y == 0)
		return false;

	// area
	float upperBound = (maxTerrainHeight - rayOrigin.y) / rayDirection.y;
	float lowerBound = (minTerrainHeight - rayOrigin.y) / rayDirection.y;

	if(lowerBound < 0.0 && upperBound < 0.0)
		return false;
	float start = max(min(upperBound, lowerBound), NearPlane);
	float end   = min(max(upperBound, lowerBound), FarPlane);

	// go!
	float lh = 0.0;
	float ly = 0.0;
	float lt = start;
	float delt;
	for(float t=start; t<end; t*=1.011)
	{
		float3 pos = rayOrigin + rayDirection * t;
        float h = getTerrainHeight(pos.xz, t * RayStepToHeightmapLod * lodFactor);

        if(pos.y - h < 0)
		{
			float laststep = t - lt;
			dist = (t - laststep + laststep*(lh-ly)/(pos.y-ly-h+lh));
			intersectionPoint = rayOrigin + rayDirection * dist;
			return true;
        }
		lt = t;
		lh = h;
		ly = pos.y;
	}
    return false;
}

// heightmap display in upper right corner
static const float heightmapCornerSize = 0.5f;
bool RenderHeightmapInCorner(float2 deviceCor, out float4 color)
{
	float2 heightmapArea = float2(heightmapCornerSize,heightmapCornerSize * ScreenAspectRatio);
	float2 onScreenHeightmapZero = float2(1.0f - heightmapArea.x, -1.0f); 
	float2 heightmapCor = (float2(deviceCor.x, -deviceCor.y) - onScreenHeightmapZero) / heightmapArea;
	if(heightmapCor.x > 0 && heightmapCor.x < 1 && heightmapCor.y > 0 && heightmapCor.y < 1)
	{
		color = float4(Heightmap.SampleLevel(LinearSampler, heightmapCor, 0).rrr, 1);
		return true;
	}
	else
	{
		color = float4(1,0,1,1);
		return false;
	}
}

static const float Ambient = 0.3f;
float4 PS(PS_INPUT input) : SV_Target
{
	float4 color;
	if(RenderHeightmapInCorner(input.DevicePos, color))
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
	float dist;

	//int steps = 0;
	float3 outColor;
	if(rayCast(CameraPosition, rayDirection, terrainPosition, dist))
	{
		// LIGHTING
		float3 normal = getTerrainNormal(terrainPosition);
		float3 null;
		float lighting = max(0, dot(normal, LightDirection));
		float distToShadowCaster;
		if(rayCast(terrainPosition, LightDirection, null, distToShadowCaster, 4.0f))
			lighting *= min(max(0.3, distToShadowCaster*0.01), 1.0);
		
		outColor = float3(1,1,1) * lighting + float3(Ambient,Ambient,Ambient);


		// FOGGING
		// clever fog http://www.iquilezles.org/www/articles/fog/fog.htm
		float fogAmount = min(1, 0.5 * exp(-CameraPosition.y  * 0.01) * (1.0 - exp( -dist*rayDirection.y* 0.01)) / rayDirection.y);
		outColor = lerp(outColor, computeSkyColor(rayDirection), fogAmount);
	}
	else
	{
		outColor = computeSkyColor(rayDirection);
	}

	return float4(outColor, 1.0f);
}

technique10 Render
{
    pass P0
    {
        SetVertexShader(CompileShader(vs_5_0, VS()));
        SetGeometryShader(CompileShader(gs_5_0, GS()));
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }
}


