#include "CommandInfo.h"
#include "math.hpp"
#include "Noise.h"

const float HORIZONTAL_NOISE_SCALE = 0.01f;
using namespace std::placeholders;

float CmdValueNoise::CalculateFrequenceAmplitude( const MapBufferInfo& bufferDesc, float _fCurrentHeight, float _fFrequence, float _fGradientX, float _fGradientY )
{
	float fHeightDependency = exp( (_fCurrentHeight-_heightDependencyOffset) * _heightDependency);
	float fGradientDependency = 1.0f + sqrt(_fGradientX*_fGradientX + _fGradientY*_fGradientY) * _gradientDependency;
	return min( 6.0f, fHeightDependency*fGradientDependency ) / _fFrequence;
}

float CmdValueNoise::NoiseKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	float fGX = 0.0f;
	float fGY = 0.0f;
	float fSum = 0.0f;
	float fx = HORIZONTAL_NOISE_SCALE * x;
	float fy = HORIZONTAL_NOISE_SCALE * y;

	float fHeightOffset = currentResult ? currentResult[y*bufferInfo.ResolutionX+x] : 0.0f;

	// *************** Noise function ***************
	for( int i=0; i<_maxOctave; ++i )
	{
		float fdX;
		float fdY;
		float fFrequence = float(1<<i);
		float fAmplitude = CalculateFrequenceAmplitude( bufferInfo, fSum+fHeightOffset, fFrequence, fGX, fGY ) * _heightScale;
		//fSum += abs(Rand2D( fx, fy, fFrequence, fdX, fdY ) - 0.5f) * fAmplitude;
		fSum += (Rand2D( fx, fy, fFrequence, fdX, fdY ) * 2.0f - 1.0f) * fAmplitude;
		// Update global gradient
		fGX += fdX*fFrequence*fAmplitude;		fGY += fdY*fFrequence*fAmplitude;
	}

	return fSum;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdValueNoise::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// **** Precomputations **** //
	_maxOctave = int(log( std::max(bufferInfo.ResolutionX, bufferInfo.ResolutionY) )/log(2));

	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdValueNoise::NoiseKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayer(Cmd);
}