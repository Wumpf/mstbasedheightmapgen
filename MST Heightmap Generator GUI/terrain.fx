cbuffer Camera
{
	float4x4 InverseViewProjection; 
	float3 CameraPosition;
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
};

struct GS_INPUT
{
};

[maxvertexcount(3)]
void GS(point GS_INPUT p[1], inout TriangleStream<PS_INPUT> TriStream )
{
	PS_INPUT vertices[3];
	vertices[0].Pos = float4(-1, 1, 0, 1);
	vertices[1].Pos = float4( 3,  1, 0, 1);
	vertices[2].Pos = float4(-1, -3, 0, 1);
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

static const float3 LightDirection = float3(-0.577,0.577,-0.577);

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
static const float terrainScale = 35;
static const float minTerrainHeight = -65;
static const float maxTerrainHeight = minTerrainHeight+terrainScale;


SamplerState LinearSampler : register( s0 );  
Texture2D Heightmap : register( t0 );  

static const float heightmapTiling = 0.004;
static const float texelSize = 1.0/512.0;

float getTerrainHeight(in float2 pos, in float cameraDistance)
{
	float lod = 1.0f;// min(9, cameraDistance*0.01);
	return Heightmap.SampleLevel(LinearSampler, pos*heightmapTiling, lod).x * terrainScale + minTerrainHeight;
}

float3 getTerrainNormal(in float3 pos)
{
	float lod = 0.0f;
	float2 texcoord = pos.xz * heightmapTiling;
    float3 n = float3(Heightmap.SampleLevel(LinearSampler, float2(texcoord.x-texelSize, texcoord.y), lod).x - 
					Heightmap.SampleLevel(LinearSampler, float2(texcoord.x+texelSize, texcoord.y), lod).x,
					texelSize*2,
					Heightmap.SampleLevel(LinearSampler, float2(texcoord.x, texcoord.y-texelSize), lod).x - 
					Heightmap.SampleLevel(LinearSampler, float2(texcoord.x, texcoord.y+texelSize), lod).x  );
    return normalize(n);
}

// ------------------------------------------------
// RAYMARCH CORE
// ------------------------------------------------
bool rayCast(in float3 rayOrigin, in float3 rayDirection, out float3 intersectionPoint, out float dist)
{
	if(rayDirection.y == 0)
		return false;

	// area
	static const float maxStep = 200;
	float upperBound = (maxTerrainHeight - rayOrigin.y) / rayDirection.y;
	float lowerBound = (minTerrainHeight - rayOrigin.y) / rayDirection.y;
	if(lowerBound < 0.0 && upperBound < 0.0)
		return false;
	float start = max(min(upperBound, lowerBound), 5);
	float end   = min(max(upperBound, lowerBound), maxStep);

	// go!
//	float stepLen = 20;
	float lh = 0.0;
	float ly = 0.0;
	float lt = start;
	for(float t=start; t<end; t*=1.011)
	{
		float3 pos = rayOrigin + rayDirection * t;
        float h = getTerrainHeight(pos.xz, t);

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

float3 PS(PS_INPUT input) : SV_Target
{
    // "picking" - compute raydirection
	float2 deviceCor = input.Pos.xy;
	float4 rayOrigin = mul(InverseViewProjection, float4(deviceCor, -1, 1));
	rayOrigin.xyz /= rayOrigin.w;
	float4 rayTarget = mul(InverseViewProjection, float4(deviceCor, 0, 1));
	rayTarget.xyz /= rayTarget.w;
	float3 rayDirection = normalize(rayTarget.xyz - rayOrigin.xyz);
	
	// Color
	float3 terrainPosition;
	float2 terrainDerivates;
	float dist;

	//int steps = 0;
	float3 outColor;
	if(false)//rayCast(CameraPosition, rayDirection, terrainPosition, dist))
	{
		// LIGHTING
		float3 normal = getTerrainNormal(terrainPosition);
		float3 null;
		float lighting = max(0, dot(normal, LightDirection));
		float distToShadowCaster;
		if(rayCast(terrainPosition+LightDirection, LightDirection, null, distToShadowCaster))
			lighting *= min(max(0.3, distToShadowCaster*0.01), 1.0);
		
		outColor = float3(1,1,1) * lighting;


		// FOGGING
		// clever fog http://www.iquilezles.org/www/articles/fog/fog.htm
		float fogAmount = min(1, 0.5 * exp(-CameraPosition.y  * 0.01) * (1.0 - exp( -dist*rayDirection.y* 0.01)) / rayDirection.y);
		outColor = lerp(outColor, computeSkyColor(rayDirection), fogAmount);
	}
	else
	{
		outColor = computeSkyColor(rayDirection);
	}

	return outColor;
}

technique10 Render
{
    pass P0
    {
        SetVertexShader(null);
        SetGeometryShader(CompileShader(gs_5_0, GS()));
        SetPixelShader(CompileShader(ps_5_0, PS()));
    }
}


