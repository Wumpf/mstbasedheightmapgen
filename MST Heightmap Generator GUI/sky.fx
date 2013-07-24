struct GS_INPUT
{
};
GS_INPUT VS()
{
	GS_INPUT output;
	return output;
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
	float3 Direction : DIRECTION;
	uint RTIndex : SV_RenderTargetArrayIndex;
};

//-X, +X, -Y, +Y, -Z, +Z
static const float3 SliceDirections[6] = {
	float3( 1,  0,  0),
	float3(-1,  0,  0),
	float3( 0,  1,  0),
	float3( 0, -1,  0),
	float3( 0,  0, -1),
	float3( 0,  0,  1),
};
static const float3 SliceRight[6] = {
	float3( 0,  0,  1),
	float3( 0,  0,  -1),
	float3( 1,  0,  0),
	float3( 0,  0,  1),
	float3( 1,  0, 0),
	float3( -1,  0,  0),
};
static const float3 SliceUp[6] = {
	float3( 0,  1,  0),
	float3( 0,  1,  0),
	float3( 0,  0,  1),
	float3(-1,  0,  0),
	float3( 0,  1,  0),
	float3( 0,  1,  0),
};


[maxvertexcount(18)]
void GS(point GS_INPUT p[1], inout TriangleStream<PS_INPUT> TriStream )
{
	PS_INPUT vertices[3];
	// screen aligned triangle!
	vertices[0].Pos = float4(-1, 1, 0, 1);
	vertices[1].Pos = float4( 3,  1, 0, 1);
	vertices[2].Pos = float4(-1, -3, 0, 1);

    [unroll] for( uint f = 0; f < 6; ++f )
    {
		vertices[0].RTIndex = f;
		vertices[1].RTIndex = f;
		vertices[2].RTIndex = f;

		vertices[0].Direction = SliceDirections[f] + vertices[0].Pos.x * SliceRight[f] + vertices[0].Pos.y * SliceUp[f];
		vertices[1].Direction = SliceDirections[f] + vertices[1].Pos.x * SliceRight[f] + vertices[1].Pos.y * SliceUp[f];
		vertices[2].Direction = SliceDirections[f] + vertices[2].Pos.x * SliceRight[f] + vertices[2].Pos.y * SliceUp[f];
	
		TriStream.Append(vertices[0]);
		TriStream.Append(vertices[1]);
		TriStream.Append(vertices[2]);
        TriStream.RestartStrip();
    }
}

// ---------------------------------------------------------------------------------
// PIXEL SHADER
// ---------------------------------------------------------------------------------

// hlsl port of http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
/*
    :copyright: 2011 by Florian Boesch <pyalot@gmail.com>.
    :license: GNU AGPL3
*/

float3 LightDirection;

static const float3 Kr = float3(0.18867780436772762, 0.4978442963618773, 0.6616065586417131); // air color
static const float rayleigh_brightness = 5;//3.3f;
static const float mie_brightness = 0.25f; // 0.20
static const float spot_brightness = 500;   // 1000
static const float scatter_strength = 28.0f/1000;
static const float rayleigh_strength = 0.139f;
static const float mie_strength = 0.0264f;
static const float rayleigh_collection_power = 0.81f;
static const float mie_collection_power = 0.39f;
static const float mie_distribution = 0.63f;
    
static const float surface_height = 0.99;
static const float range = 0.01;
static const float intensity = 1.8;
static const int step_count = 16;

float atmospheric_depth(float3 position, float3 dir)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, position);
    float c = dot(position, position)-1.0;
    float det = b*b-4.0*a*c;
    float detSqrt = sqrt(det);
    float q = (-b - detSqrt)/2.0;
    float t1 = c/q;
    return t1;
}

float phase(float alpha, float g)
{
    float a = 3.0*(1.0-g*g);
    float b = 2.0*(2.0+g*g);
    float c = 1.0+alpha*alpha;
    float d = pow(1.0+g*g-2.0*g*alpha, 1.5);
    return (a/b)*(c/d);
}

float horizon_extinction(float3 position, float3 dir, float radius)
{
    float u = dot(dir, -position);
    if(u<0.0)
	{
        return 1.0;
    }
    float3 near = position + u*dir;
    if(length(near) < radius)
	{
        return 0.0;
    }
    else
	{
        float3 v2 = normalize(near)*radius - position;
        float diff = acos(dot(normalize(v2), dir));
        return smoothstep(0.0, 1.0, pow(diff*2.0, 3.0));
    }
}

float3 absorb(float dist, float3 color, float factor)
{
    return color-color*pow(Kr, factor/dist);
}

float3 PS(PS_INPUT input) : SV_Target
{
    float3 eyedir = normalize(input.Direction);
    float alpha = dot(eyedir, LightDirection);
        
    float rayleigh_factor = phase(alpha, -0.01)*rayleigh_brightness;
    float mie_factor = phase(alpha, mie_distribution)*mie_brightness;
    float spot = smoothstep(0.0, 15.0, phase(alpha, 0.9995))*spot_brightness;

    float3 eye_position = float3(0.0, surface_height, 0.0);
    float eye_depth = atmospheric_depth(eye_position, eyedir);
    float step_length = eye_depth/float(step_count);
    float eye_extinction = horizon_extinction(eye_position, eyedir, surface_height-0.15);
        
    float3 rayleigh_collected = float3(0.0, 0.0, 0.0);
    float3 mie_collected = float3(0.0, 0.0, 0.0);

    for(int i=0; i<step_count; i++)
	{
        float sample_distance = step_length*float(i);
        float3 position = eye_position + eyedir*sample_distance;
        float extinction = horizon_extinction(position, LightDirection, surface_height-0.35);
        float sample_depth = atmospheric_depth(position, LightDirection);
        float3 influx = absorb(sample_depth, float3(intensity,intensity,intensity), scatter_strength)*extinction;
        rayleigh_collected += absorb(sample_distance, Kr*influx, rayleigh_strength);
        mie_collected += absorb(sample_distance, influx, mie_strength);
    }

    rayleigh_collected = (rayleigh_collected*eye_extinction*pow(eye_depth, rayleigh_collection_power))/float(step_count);
    mie_collected = (mie_collected*eye_extinction*pow(eye_depth, mie_collection_power))/float(step_count);

    return float3(spot*mie_collected + mie_factor*mie_collected + rayleigh_factor*rayleigh_collected);
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
