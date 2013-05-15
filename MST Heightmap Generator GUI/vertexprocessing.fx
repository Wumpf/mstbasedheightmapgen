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