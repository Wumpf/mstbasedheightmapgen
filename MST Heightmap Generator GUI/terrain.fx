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
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
	float2 DevicePos : TEXCOORD0;
};

struct GS_INPUT
{
};


GS_INPUT VS()
{
	GS_INPUT output;
	return output;
}

[maxvertexcount(3)]
void GS(point GS_INPUT p[1], inout TriangleStream<PS_INPUT> TriStream )
{
	PS_INPUT vertices[3];
	vertices[0].Pos = float4(-1, 1, 0, 1);
	vertices[1].Pos = float4( 3,  1, 0, 1);
	vertices[2].Pos = float4(-1, -3, 0, 1);
	vertices[0].DevicePos = vertices[0].Pos.xy;
	vertices[1].DevicePos = vertices[1].Pos.xy;
	vertices[2].DevicePos = vertices[2].Pos.xy;
	TriStream.Append(vertices[0]);
	TriStream.Append(vertices[1]);
	TriStream.Append(vertices[2]);
	TriStream.RestartStrip();
}

// ------------------------------------------------
// SKY
// ------------------------------------------------
static const float3 AdditonalSunColor = float3(1.0, 0.98, 0.8)/3;
static const float3 LowerHorizonColour = float3(0.815, 1.141, 1.54)/2;
static const float3 UpperHorizonColour = float3(0.986, 1.689, 2.845)/2;
static const float3 UpperSkyColour = float3(0.16, 0.27, 0.43)*0.8;
static const float3 GroundColour = float3(0.31, 0.41, 0.5)*0.8;
static const float LowerHorizonHeight = -0.4;
static const float UpperHorizonHeight = -0.1;
static const float SunAttenuation = 2;

static const float3 LightDirection = float3(-0.577, 0.577, -0.577);

float3 computeSkyColor(in float3 ray)
{
	float3 color;

	// background
	float heightValue = ray.y;	// mirror..
	if(heightValue < LowerHorizonHeight)
		color = lerp(GroundColour, LowerHorizonColour, (heightValue+1) / (LowerHorizonHeight+1));
	else if(heightValue < UpperHorizonHeight)
		color = lerp(LowerHorizonColour, UpperHorizonColour, (heightValue-LowerHorizonHeight) / (UpperHorizonHeight - LowerHorizonHeight));
	else
		color = lerp(UpperHorizonColour, UpperSkyColour, (heightValue-UpperHorizonHeight) / (1.0-UpperHorizonHeight));
	
	// Sun
	float angle = max(0, dot(ray, LightDirection));
	color += (pow(angle, SunAttenuation) + pow(angle, 10000)*10) * AdditonalSunColor;

	return color;
}

// ------------------------------------------------
// TERRAIN
// ------------------------------------------------
static const float terrainScale = 40;
static const float maxTerrainHeight = terrainScale;

SamplerState LinearSampler;  
Texture2D Heightmap;  

float getTerrainHeight(in float2 pos)
{
	return Heightmap.SampleLevel(LinearSampler, pos*HeightmapSizeInv, 0).x * terrainScale;
}

float3 getTerrainNormal(in float3 pos)
{
	float4 h;
	float2 texcoord = pos.xz * HeightmapSizeInv;
	h[0] = Heightmap.SampleLevel(LinearSampler, texcoord + HeightmapSizeInv*float2( 0,-1), 0).r;
	h[1] = Heightmap.SampleLevel(LinearSampler, texcoord + HeightmapSizeInv*float2( 0, 1), 0).r;
	h[2] = Heightmap.SampleLevel(LinearSampler, texcoord + HeightmapSizeInv*float2( 1, 0), 0).r;
	h[3] = Heightmap.SampleLevel(LinearSampler, texcoord + HeightmapSizeInv*float2(-1, 0), 0).r;
	h *= terrainScale;
	float3 vecdz = float3(0.0f, h[1] - h[0], 2);
	float3 vecdx = float3(2, h[2] - h[3], 0.0f);
    return normalize(cross(vecdz, vecdx));
}

// ------------------------------------------------
// RAYMARCH CORE
// ------------------------------------------------
static const float maxStep = 1000;
bool rayCast(in float3 rayOrigin, in float3 rayDirection, out float3 intersectionPoint, out float dist)
{
	if(rayDirection.y == 0)
		return false;

	// area
	float upperBound = (maxTerrainHeight - rayOrigin.y) / rayDirection.y;
	float lowerBound = (0				 - rayOrigin.y) / rayDirection.y;
	if(lowerBound < 0.0 && upperBound < 0.0)
		return false;
	float start = max(min(upperBound, lowerBound), 5);
	float end   = min(max(upperBound, lowerBound), maxStep);

	// go!
	float lh = 0.0;
	float ly = 0.0;
	float lt = start;
	for(float t=start; t<end; t*=1.011)
	{
		float3 pos = rayOrigin + rayDirection * t;
        float h = getTerrainHeight(pos.xz);

        if(pos.y - h < 0)
		{
			float laststep = t - lt;
			dist = (t - laststep + laststep*(lh-ly)/(pos.y-ly-h+lh));
			intersectionPoint = rayOrigin + rayDirection * dist;
			return true;
        }
		lt = t;
	//	stepLen = 0.012 * t;	// addaptive error
		lh = h;
		ly = pos.y;
	}
    return false;
}


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
		if(rayCast(terrainPosition+LightDirection, LightDirection, null, distToShadowCaster))
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


