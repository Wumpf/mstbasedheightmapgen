#include "CommandInfo.h"
#include "math.hpp"
#include "Noise.h"

using namespace std::placeholders;

inline float smoothstep(float x) { x = saturate(x); return x*x*(3 - 2*x); }
inline float semistep(float x) { x = saturate(x); return x*x; }

static float Voronoise( float x, float y )
{
	const int R = 2;
	float fFracX, fFracY;
	int iX, iY;
	iX = Floor(x);	iY = Floor(y);
	fFracX = x - iX;	fFracY = y - iY;
	float dummy;

	float fValue = 0.0f;
	float fWeightSum = 0.0f;
	for( int j=-R; j<=R; j++ )
	for( int i=-R; i<=R; i++ )
	{
		float fJitterX = (float)Sample2D((iX+i) ^ 0x7a2f5af8afd0d7e0, (iY+j) ^ 0xdbb8d9f9d5e3d6be);
		float fJitterY = (float)Sample2D((iX+i) ^ 0xde9d8b67ca23a61d, (iY+j) ^ 0x172ffe84e2e5a30c);
		float fNoise = (float)Sample2D(iX+i, iY+j);
		float fEdgeNoise = max(0.0f, 0.01f + 0.01f * Rand2D(0, 0, 0.5f, x * 5.0f, y * 5.0f, dummy, dummy));
		float rx = i + fJitterX - fFracX;
		float ry = j + fJitterY - fFracY;
		float dist = sqrt(rx*rx + ry*ry);
		//float w = smoothstep(1.0f - dist / sqrt(2.0f));
		float w = max(0.0f, 1.0f - dist / 2.0f);
		w = max(0.0f, w - fEdgeNoise / (w + 0.2f));
		fValue += w * fNoise;
		fWeightSum += w;
	}

	return fValue/fWeightSum;
}

float CmdVoronoise::NoiseKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	float fSum = 0.0f;
	float fx = _noiseScaleX * x;
	float fy = _noiseScaleY * y;

	// *************** Noise function ***************
	for( int i=_minOctave; i<=_maxOctave; ++i )
	{
		float fFrequence = float(1<<i);
		float fAmplitude = 1.0f / pow(fFrequence, 1.3f);
		fSum += (Voronoise( fx * fFrequence, fy * fFrequence ) * 2.0f - 1.0f) * fAmplitude;
	}

	return fSum;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdVoronoise::Execute( const MapBufferInfo& bufferInfo,
							const float* prevResult,
							const float* currentResult,
							float* destination)
{
	// **** Precomputations **** //
	_noiseScaleX = 5.0f / bufferInfo.ResolutionX;
	_noiseScaleY = 5.0f / bufferInfo.ResolutionY;

	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdVoronoise::NoiseKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayer(Cmd);
}