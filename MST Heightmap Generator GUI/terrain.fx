#include "vertexprocessing.fx"

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
	float TerrainScale;
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
	h[0] = getTerrainHeight(pos.xz + float2( 0,-HeightmapPixelSizeInWorld.y ));
	h[1] = getTerrainHeight(pos.xz + float2( 0, HeightmapPixelSizeInWorld.y ));
	h[2] = getTerrainHeight(pos.xz + float2( HeightmapPixelSizeInWorld.x, 0 ));
	h[3] = getTerrainHeight(pos.xz + float2(-HeightmapPixelSizeInWorld.x, 0 ));
	//float3 vecdz = float3(0.0f, h[1] - h[0], 2);
	//float3 vecdx = float3(2, h[2] - h[3], 0.0f);
    return normalize(float3(-h[2] + h[3], 2.0, -h[1] + h[0]));
}

// ------------------------------------------------
// RAYMARCH CORE
// ------------------------------------------------
static const float FarPlane = 1000;
static const float NearPlane = 3;
static const float RayStepToHeightmapLod = 0.01f;
static const float SoftShadowFactor = 200.0f;	// lower means softer
bool rayCast(in float3 rayOrigin, in float3 rayDirection, out float3 intersectionPoint, out float shadowTerm)
{
//	if(abs(rayDirection.y) < 0.000001)
//		return false;

	float upperBound = (TerrainScale	 - rayOrigin.y) / rayDirection.y;
	float lowerBound = (0			     - rayOrigin.y) / rayDirection.y;
	// Clip if ray upward from above or downward from below
	if(lowerBound < 0.0 && upperBound < 0.0)
		return false;
	float start = max(min(upperBound, lowerBound), NearPlane);
//	float end   = min(max(upperBound, lowerBound), FarPlane);

	// ray direction in texture space
	float3 rayDirection_TextureSpace;
	rayDirection_TextureSpace.xz = rayDirection.xz * WorldUnitToHeightmapTexcoord;
	rayDirection_TextureSpace.y = rayDirection.y / TerrainScale;
//	rayDirection_TextureSpace = normalize(rayDirection_TextureSpace);	// this makes min offset easier // wrong!
//	rayDirection_TextureSpace /= rayDirection_TextureSpace.y;	// should simplify computations
	float dirXZlen = length(rayDirection_TextureSpace.xz);

	// starting point
	intersectionPoint = rayOrigin + rayDirection * start;
	intersectionPoint.xz = intersectionPoint.xz * WorldUnitToHeightmapTexcoord + 0.5f;
	float2 startOffset = (saturate(intersectionPoint.xz)-intersectionPoint.xz)/rayDirection_TextureSpace.xz;
	float startOffsetMax = max(startOffset.x, startOffset.y);
	intersectionPoint.y = intersectionPoint.y / TerrainScale;
	intersectionPoint += startOffsetMax* rayDirection_TextureSpace;

	// cone stepping
	shadowTerm = 1.0f;
	float lastShadowTerm = 1.0f;

	const int MaxNumConeSteps = 150;
	float minDistOffset = HeightmapSizeInv.x / dirXZlen * 0.05;
	float distOffset;
	float deltaHeight;
	float travelled = 0.0;
	float softShadowingTravelFactor = SoftShadowFactor / dirXZlen;
	for(int i = 0; i < MaxNumConeSteps; ++i)
	{
		float2 height_cone = Heightmap.SampleLevel(TerrainHeightmapSampler, intersectionPoint.xz, 0).xy;
		deltaHeight = intersectionPoint.y - height_cone.x;

		// step cone
		distOffset = deltaHeight * height_cone.y / (dirXZlen - rayDirection_TextureSpace.y * height_cone.y);
		distOffset = max(minDistOffset, distOffset);
		intersectionPoint += rayDirection_TextureSpace * distOffset;

		// outside?
		if(any(intersectionPoint.xz != saturate(intersectionPoint.xz)) || (intersectionPoint.y > 1))
			return false;

		// below terrain? done.
		if(deltaHeight < 0.0)
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
}

// heightmap display in upper right corner
static const float HeightmapCornerSize = 0.5f;
bool RenderHeightmapInCorner(float2 deviceCor, out float4 color)
{
	float2 heightmapArea = float2(HeightmapCornerSize,HeightmapCornerSize * ScreenAspectRatio);
	float2 onScreenHeightmapZero = float2(1.0f - heightmapArea.x, -1.0f); 
	float2 heightmapCor = (float2(deviceCor.x, -deviceCor.y) - onScreenHeightmapZero) / heightmapArea;
	if(heightmapCor.x > 0 && heightmapCor.x < 1 && heightmapCor.y > 0 && heightmapCor.y < 1)
	{
		color = float4(Heightmap.SampleLevel(TerrainHeightmapSampler, heightmapCor, 0).rg, 0, 1);
		return true;
	}
	else
	{
		color = float4(1,0,1,1);
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

	//int steps = 0;
	float3 outColor;
	float shadowTerm;
	if(rayCast(CameraPosition, rayDirection, terrainPosition, shadowTerm))
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

		outColor = float3(0.9,0.9,0.88) * lighting;//SampleSky(normal)*0.4;

		// FOGGING
		// clever fog http://www.iquilezles.org/www/articles/fog/fog.htm
		float dist = length(terrainPosition - rayOrigin);
		float fogAmount = min(1, 0.5 * exp(-CameraPosition.y  * 0.01) * (1.0 - exp( -dist*rayDirection.y* 0.011)) / rayDirection.y);
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
        SetVertexShader(CompileShader(vs_5_0, VS()));
        SetGeometryShader(CompileShader(gs_5_0, GS()));
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }
}


