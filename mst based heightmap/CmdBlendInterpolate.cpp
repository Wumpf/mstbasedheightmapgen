#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

void CmdBlendInterpolate::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// Blending must have at least the one source
	assert( currentResult );

	// **** Per pixel **** //
	if( prevResult )
	{
		CommandDesc Cmd(bufferInfo, prevResult, currentResult,
			[&](const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult)
				{ return lrp(prevResult[y*bufferInfo.ResolutionX+x], currentResult[y*bufferInfo.ResolutionX+x], _blendFactor); },
			destination);
		GenerateLayer(Cmd);
	}
}