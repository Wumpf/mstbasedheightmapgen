#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

float CmdBlendMultiply::BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	return currentResult[y*bufferInfo.ResolutionX+x];
}

float CmdBlendMultiply::BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	return currentResult[y*bufferInfo.ResolutionX+x] * prevResult[y*bufferInfo.ResolutionX+x];
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdBlendMultiply::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// Blending must have at least the one source
	assert( currentResult );

	// **** Precomputations **** //
	// None

	// **** Per pixel **** //
	Kernel_t kernel;
	if( prevResult ) kernel = std::bind(&CmdBlendMultiply::BlendKernel, this, _1, _2, _3, _4, _5);
	else kernel = std::bind(&CmdBlendMultiply::BlendKernelNeutral, this, _1, _2, _3, _4, _5);
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		kernel,
		destination);

	GenerateLayer(Cmd);
}