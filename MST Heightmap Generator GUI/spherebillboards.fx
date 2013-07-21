//float3 CameraUp;
//float3 CameraRight;
float3 CameraPosition;
float HeightScale;
float3 Translation;
matrix WorldViewProjection;

struct ParticleVertex
{
    float3 Position : POSITION;
};

struct ParticleVertexGsOut
{
    float4 Position : SV_POSITION;
	float2 Texcoord : TEXCOORD0;
};


ParticleVertex VS_Passthrough(ParticleVertex v)
{
	return v;
}

[maxvertexcount(4)]
void GS_Render(point ParticleVertex inputArray[1], inout TriangleStream<ParticleVertexGsOut> stream) {

	ParticleVertex input = inputArray[0];
    ParticleVertexGsOut v;

	const float SIZE = 2.0f;

	float3 position = input.Position + Translation;
	position.y *= HeightScale;

	float3 toCamera = position-CameraPosition;
	float3 right = normalize(cross(toCamera,float3(0,1,0))) * SIZE;//CameraRight * SIZE;
	float3 up = normalize(cross(right, toCamera)) * SIZE;

	// Create and append four vertices to form a quad.
    float4 positionWS = float4(position + right + up, 1.f);
	v.Position = mul(positionWS, WorldViewProjection);
	v.Texcoord = float2(1, 1);
    stream.Append(v);

    positionWS = float4(position - right + up, 1.f);
    v.Position = mul(positionWS, WorldViewProjection);
	v.Texcoord = float2(0, 1);
    stream.Append(v);

    positionWS = float4(position + right - up, 1.f);
    v.Position = mul(positionWS, WorldViewProjection);
	v.Texcoord = float2(1, 0);
    stream.Append(v);

    positionWS = float4(position - right - up, 1.f);
    v.Position = mul(positionWS, WorldViewProjection);
	v.Texcoord = float2(0, 0);
    stream.Append(v);

    stream.RestartStrip();
}

// Simple pixel shader to render the particles.
float4 PS_Render(ParticleVertexGsOut input) : SV_Target
{
	float2 vecToMid = float2(1.0f, 1.0f) - input.Texcoord*2;
	float intens = dot(vecToMid,vecToMid);
	clip(1.0f-intens);
	return float4(intens*0.5f, intens, intens*0.5f, intens);
}

technique RenderTeq
{
    pass Pass1
    {
		Profile = 10.0;
        VertexShader = VS_Passthrough;
		GeometryShader = GS_Render;
        PixelShader = PS_Render;
    }
}