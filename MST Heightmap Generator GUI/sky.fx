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

static const float3 LightDirection = float3(0.666666, 0.3333333, 0.666666); // float3(0.471929, 0.849473, 0.235965);
//static const float3 LightDirection = float3(-0.707, 0.707, 0.0);
//static const float3 LightDirection = float3(0.0, 0.707, -0.707);
//static const float3 LightDirection = float3(0.0, 1.0, 0.0);

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
