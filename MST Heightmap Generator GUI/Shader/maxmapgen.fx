#include "screenalignedtriangle.fx"

SamplerState NearestSampler;
Texture2D InputTexture;

float PS(PS_INPUT input) : SV_Target
{
  float4 values = InputTexture.GatherRed(NearestSampler, input.Texcoord);
  return max(max(max(values.x, values.y), values.z), values.w);
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