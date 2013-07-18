#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

float CmdBlendRefract::BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	return currentResult[y*bufferInfo.ResolutionX+x];
}

float CmdBlendRefract::BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	// Compute gradient with finite difference on currentResult.
	Vec3 vGradient = nrm(Vec3(
		currentResult[y*bufferInfo.ResolutionX + min(bufferInfo.ResolutionX-1,x+1)] - currentResult[y*bufferInfo.ResolutionX + max(0,x-1)],
		2.0f,
		currentResult[min(bufferInfo.ResolutionY-1,y+1)*bufferInfo.ResolutionX + x] - currentResult[max(0,y-1)*bufferInfo.ResolutionX + x]
		));

	// Compute destortion source position
	float fStep = _refractionDistance / vGradient.y;
	float x_refrac = max(0.0f,min(float(bufferInfo.ResolutionX-1), x + vGradient.x * fStep));
	float y_refrac = max(0.0f,min(float(bufferInfo.ResolutionY-1), y + vGradient.z * fStep));
	int dx = Floor(x_refrac);	x_refrac -= dx;
	int dy = Floor(y_refrac);	y_refrac -= dy;

	// Sample prevResult linear at the destorted position
	return lrp(lrp(prevResult[dy * bufferInfo.ResolutionX + dx], prevResult[dy * bufferInfo.ResolutionX + dx + 1], x_refrac),
			   lrp(prevResult[(dy+1) * bufferInfo.ResolutionX + dx], prevResult[(dy+1) * bufferInfo.ResolutionX + dx + 1], x_refrac), y_refrac);

	return currentResult[y*bufferInfo.ResolutionX+x] + prevResult[y*bufferInfo.ResolutionX+x];
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdBlendRefract::Execute( const MapBufferInfo& bufferInfo,
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
	if( prevResult ) kernel = std::bind(&CmdBlendRefract::BlendKernel, this, _1, _2, _3, _4, _5);
	else kernel = std::bind(&CmdBlendRefract::BlendKernelNeutral, this, _1, _2, _3, _4, _5);
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		kernel,
		destination);

	GenerateLayer(Cmd);
}