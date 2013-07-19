Texture2D<float> HeightInput;
RWTexture2D<float> ConesOutput;
RWTexture2D<float2> CombinedOutput;
SamplerState LinearSampler;

int2 TextureAreaMin;

static const uint2 AreaPerCall = uint2(32,32);
groupshared float heightValues[AreaPerCall.x][AreaPerCall.y];

[numthreads(AreaPerCall.x, AreaPerCall.y, 1)]
void CSMain( uint3 dispatchThreadID : SV_DispatchThreadID,
			 uint3 groupThreadID : SV_GroupThreadID)
{
	uint2 textureSize;
	HeightInput.GetDimensions(textureSize.x, textureSize.y);
	float2 pixelSize = float2(1.0f / (textureSize.x), 1.0f / (textureSize.y));

	// read height into shared mem
	heightValues[groupThreadID.x][groupThreadID.y] = HeightInput[TextureAreaMin + groupThreadID.xy];
	// sync
	GroupMemoryBarrierWithGroupSync();

	// own pixel height
	float srcHeight = HeightInput[dispatchThreadID.xy];
	// load cone ratio
	float coneRatio = ConesOutput[dispatchThreadID.xy];

	// loop over pixels and compute cones
	int2 curRelPix;
	int2 srcOffset = TextureAreaMin - (int2)dispatchThreadID.xy;
	for(curRelPix.x = 0; curRelPix.x < AreaPerCall.x; ++curRelPix.x)
	{
		float2 toDstTexcoord;
		toDstTexcoord.x = (curRelPix.x + srcOffset.x) * pixelSize.x;	// precompute x component
		
		for(curRelPix.y = 0; curRelPix.y < AreaPerCall.y; ++curRelPix.y)
		{
			float dstHeight = heightValues[curRelPix.x][curRelPix.y];	// read from shared mem
			[flatten]if(srcHeight < dstHeight)
			{	
				toDstTexcoord.y = (curRelPix.y + srcOffset.y) * pixelSize.y;	// missing y component
				float deltaHeight = dstHeight - srcHeight;
				float cone = dot(toDstTexcoord, toDstTexcoord) / (deltaHeight*deltaHeight);
				coneRatio = min(cone, coneRatio);
			}
		}
	}
		
	// output
	//coneRatio = sqrt(coneRatio);
	ConesOutput[dispatchThreadID.xy] = coneRatio;
}  

[numthreads(32, 32, 1)]
void CombineTextures( uint3 threadID : SV_DispatchThreadID )
{
	const float CONE_BIAS = 0.0f;//0.02f;
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