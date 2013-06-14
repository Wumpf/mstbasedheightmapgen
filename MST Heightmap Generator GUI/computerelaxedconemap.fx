
Texture2D<float> HeightInput;
RWTexture2D<float2> HeightWithConesOutput;
SamplerState LinearSampler;

[numthreads(32, 32, 1)]
void CSMain( uint3 threadID : SV_DispatchThreadID )
{
	uint2 textureSize;
	HeightInput.GetDimensions(textureSize.x, textureSize.y);
	float2 pixelSize = float2(1.0f / textureSize.y, 1.0f / textureSize.y);

	float2 srcTexcoord = pixelSize * threadID.xy;
	float srcHeight = HeightInput[threadID.xy];

	const float MAX_CONE_RATIO_SQ = 1.0f;
	float coneRatio = MAX_CONE_RATIO_SQ;

	// relaxed conemap - new algo: choose limted amount of directions, walk along them and make cone wider and wider
	const int NUM_DIRECTIONS = 80;
	//const int NUM_STEPS_PER_DIRECTION = 256;
	const float PI2 = 6.28318530718f;
	const float ANGLE_STEP = 6.28318530718f / NUM_DIRECTIONS;
	const float STEP_LEN = 0.3f;

	float stepLen = length(pixelSize) * STEP_LEN;
	

	// loop over directions
	for(float angle = 0.0f; angle<PI2; angle+=ANGLE_STEP)
	{
		float2 dirStep = float2(cos(angle), sin(angle)) * stepLen;
		
		float2 pos = srcTexcoord;
		float startHeight = HeightInput.SampleLevel(LinearSampler, pos, 0.0f);
		float lastHeight = startHeight;

		bool ascending = false;

		// run along one direction until a local max is found
		for(; !any(pos != saturate(pos)); pos += dirStep)
		{
			float height = HeightInput.SampleLevel(LinearSampler, pos, 0.0f);
			if(height > lastHeight)
				ascending = true;
			
			// now we're falling again after increasing height - that means there is a max! compute cone!
			else if(ascending)
			{
				float2 toDst = pos - srcTexcoord - dirStep;
				float deltaHeight = lastHeight - startHeight;
				coneRatio = min(dot(toDst,toDst) / (deltaHeight*deltaHeight), coneRatio);
				ascending = false;
			}

			lastHeight = height;
		}
	}


	// CONEMAP, NO RELAXING
	
/*	const int AREA_SIZE = 100;

	// loop over all pixels...
	int2 dstTexPosMin = max(int2(0,0), threadID.xy - AREA_SIZE);
	int2 dstTexPosMax = min((int2)textureSize, threadID.xy + AREA_SIZE);
	int2 dstTexPos;
	for(dstTexPos.y = dstTexPosMin.y; dstTexPos.y < dstTexPosMax.y; ++dstTexPos.y)
	{
		for(dstTexPos.x = dstTexPosMin.x; dstTexPos.x < dstTexPosMax.x; ++dstTexPos.x)
		{
			float dstHeight = HeightInput[dstTexPos];
			[flatten]if(srcHeight < dstHeight)
			{
				float2 toDstTexcoord = pixelSize * (float2)dstTexPos - srcTexcoord;
				float deltaHeight = dstHeight - srcHeight;
				coneRatio = min(dot(toDstTexcoord, toDstTexcoord) / (deltaHeight*deltaHeight), coneRatio);
			}
		}
	}
*/
		
	// output
	coneRatio = sqrt(coneRatio);
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