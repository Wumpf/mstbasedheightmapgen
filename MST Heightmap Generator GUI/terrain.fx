cbuffer cbConstant
{
    float3 vLightDir = float3(-0.577,0.577,-0.577);
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
};

[maxvertexcount(6)]
void GS(inout TriangleStream<PS_INPUT> TriStream )
{
	PS_INPUT vertices[4];
	vertices[0].Pos = float4(-1,  1, 0, 1);
	vertices[1].Pos = float4( 1,  1, 0, 1);
	vertices[2].Pos = float4(-1, -1, 0, 1);
	vertices[3].Pos = float4( 1, -1, 0, 1);

	TriStream.Append(vertices[0]);
	TriStream.Append(vertices[1]);
	TriStream.Append(vertices[3]);
	TriStream.RestartStrip();
	TriStream.Append(vertices[1]);
	TriStream.Append(vertices[2]);
	TriStream.Append(vertices[3]);
	TriStream.RestartStrip();}

float4 PS(PS_INPUT input) : SV_Target
{
    return float4(1,0,1,1);
}

technique10 Render
{
    pass P0
    {
        SetVertexShader(null);
        SetGeometryShader(CompileShader(gs_4_0, GS()));
        SetPixelShader(CompileShader(ps_4_0, PS()));
    }
}


