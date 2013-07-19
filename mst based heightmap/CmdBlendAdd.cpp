#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

float CmdBlendAdd::BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	return currentResult[y*bufferInfo.ResolutionX+x];
}

float CmdBlendAdd::BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	return currentResult[y*bufferInfo.ResolutionX+x] + prevResult[y*bufferInfo.ResolutionX+x];
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdBlendAdd::Execute( const MapBufferInfo& bufferInfo,
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
	if( prevResult ) kernel = std::bind(&CmdBlendAdd::BlendKernel, this, _1, _2, _3, _4, _5);
	else kernel = std::bind(&CmdBlendAdd::BlendKernelNeutral, this, _1, _2, _3, _4, _5);
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		kernel,
		destination);

	GenerateLayer(Cmd);
}