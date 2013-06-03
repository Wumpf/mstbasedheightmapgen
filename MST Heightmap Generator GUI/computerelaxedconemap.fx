
Texture2D<float> HeightInput;
RWTexture2D<float2> HeightWithConesOutput;

[numthreads(32, 32, 1)]
void CSMain( uint3 threadID : SV_DispatchThreadID )
{
	uint2 textureSize;
	HeightInput.GetDimensions(textureSize.x, textureSize.y);
	float2 pixelSize = float2(1.0f / textureSize.y, 1.0f / textureSize.y);

	float2 srcTexcoord = pixelSize * threadID.xy;
	float srcHeight = HeightInput[threadID.xy];

	const int AREA_SIZE = 200;
	const float MAX_CONE_RATIO = 1.0f;

	// CONEMAP ATM
	// NO RELAXING CURRENTLY

	// loop over all pixels...
	int2 dstTexPosMin = max(int2(0,0), threadID.xy - AREA_SIZE);
	int2 dstTexPosMax = min((int2)textureSize, threadID.xy + AREA_SIZE);
	int2 dstTexPos;
	float coneRatio = MAX_CONE_RATIO;
	for(dstTexPos.y = dstTexPosMin.y; dstTexPos.y < dstTexPosMax.y; ++dstTexPos.y)
	{
		for(dstTexPos.x = dstTexPosMin.x; dstTexPos.x < dstTexPosMax.x; ++dstTexPos.x)
		{
			float dstHeight = HeightInput[dstTexPos];
			[flatten]if(srcHeight < dstHeight)
			{
				float2 dstTexcoord = pixelSize * (float2)dstTexPos;
				coneRatio = min(length(dstTexcoord - srcTexcoord) / (dstHeight - srcHeight), coneRatio);
			}
		}
	}


	// output
	HeightWithConesOutput[threadID.xy] = float2(srcHeight, coneRatio);
}  

technique10 Compute
{
    pass P0
    {
        Profile = 11.0;
		ComputeShader = CSMain;
    }
}