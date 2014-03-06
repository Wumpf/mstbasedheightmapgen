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
    Profile = 11.0;
    VertexShader = VS;
    HullShader = null;
    DomainShader = null;
    GeometryShader = GS;
    PixelShader = PS;
  }
}