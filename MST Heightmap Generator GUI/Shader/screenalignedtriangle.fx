struct PS_INPUT
{
    float4 Pos : SV_POSITION;
	float2 DevicePos : TEXCOORD0;
  float2 Texcoord : TEXCOORD1;
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
void GS(point GS_INPUT p[1], inout TriangleStream<PS_INPUT> TriStream)
{
  PS_INPUT vertices[3];
  vertices[0].Pos = float4(-1, 1, 0, 1);
  vertices[1].Pos = float4(3, 1, 0, 1);
  vertices[2].Pos = float4(-1, -3, 0, 1);
  [flatten]for (int i = 0; i < 3; ++i)
  {
    vertices[i].DevicePos = vertices[i].Pos.xy;
    vertices[i].Texcoord = float2(vertices[i].Pos.x * 0.5f + 0.5f, 0.5f - vertices[i].Pos.y * 0.5f);
    TriStream.Append(vertices[i]);
  }
  
	TriStream.RestartStrip();
}