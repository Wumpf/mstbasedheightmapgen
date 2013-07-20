#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

void CmdBlendOverwrite::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// Blending must have at least the one source
	assert( currentResult );

	// **** Precomputations **** //
	// None

	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		[](const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult)
			{ return currentResult[y*bufferInfo.ResolutionX+x]; },
		destination);
	GenerateLayer(Cmd);
}