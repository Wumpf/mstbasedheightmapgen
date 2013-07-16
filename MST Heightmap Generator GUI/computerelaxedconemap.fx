Texture2D<float> HeightInput;
RWTexture2D<float> ConesOutput;
RWTexture2D<float2> CombinedOutput;
SamplerState LinearSampler;

int2 TextureAreaMin;

static const int2 AreaPerCall = int2(16,16);

[numthreads(16, 16, 1)]
void CSMain( uint3 threadID : SV_DispatchThreadID )
{
	uint2 textureSize;
	HeightInput.GetDimensions(textureSize.x, textureSize.y);

	float2 pixelSize = float2(1.0f / textureSize.y, 1.0f / textureSize.y);
	float2 srcTexcoord = pixelSize * threadID.xy;

	// pixel height
	float srcHeight = HeightInput[threadID.xy];
	// load cone ratio
	float coneRatio = ConesOutput[threadID.xy];

	const float MAXCHECK_STEP_LEN = 0.01f;
	float maxcheckStepLen = length(pixelSize) * MAXCHECK_STEP_LEN;

	// loop over pixels and compute rays to them
	int2 dstTexPos;
	int2 dstTexPosMax = TextureAreaMin + AreaPerCall;
	for(dstTexPos.y = TextureAreaMin.y; dstTexPos.y < dstTexPosMax.y; ++dstTexPos.y)
	{
		for(dstTexPos.x = TextureAreaMin.x; dstTexPos.x < dstTexPosMax.x; ++dstTexPos.x)
		{
			float dstHeight = HeightInput[dstTexPos];
			[flatten]if(srcHeight < dstHeight)
			{
				float2 dstTexcoord = pixelSize * (float2)dstTexPos;
				float2 toDstTexcoord = dstTexcoord - srcTexcoord;

				// is this pixel a max?
				float2 step = normalize(toDstTexcoord) * maxcheckStepLen;
				float forward = HeightInput.SampleLevel(LinearSampler, dstTexcoord + step, 0.0f);
				float backward = HeightInput.SampleLevel(LinearSampler, dstTexcoord - step, 0.0f);
				[flatten]if(forward < dstHeight && backward < dstHeight)	// this should be sufficient for relaxing
				{
					float deltaHeight = dstHeight - srcHeight;
					coneRatio = min(dot(toDstTexcoord, toDstTexcoord) / (deltaHeight*deltaHeight), coneRatio);
				}
			}
		}
	}

		
	// output
	//coneRatio = sqrt(coneRatio);
	ConesOutput[threadID.xy] = coneRatio;
}  

[numthreads(16, 16, 1)]
void CombineTextures( uint3 threadID : SV_DispatchThreadID )
{
	const float CONE_BIAS = 0.02f;
	CombinedOutput[threadID.xy] = float2(HeightInput[threadID.xy], sqrt(ConesOutput[threadID.xy]) - CONE_BIAS);
}

technique10 Compute
{
    pass P0
    {
        Profile = 11.0;
		ComputeShader = CSMain;
    }
}

technique10 Combine
{
    pass P0
    {
        Profile = 11.0;
		ComputeShader = CombineTextures;
    }
}