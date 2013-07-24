#include <thread>
#include "CommandInfo.h"
#include "math.hpp"
#include "Generator.h"

using namespace std::placeholders;

// Stuff copied from OrBaseLib
#include "src-mst/OrADTObjects.h"
#include "src-mst/OrHeap.h"
#include "src-mst/OrHash.h"
#include "src-mst/OrGraph.h"
typedef OrE::ADT::Mesh::PosNode PNode;

// ************************************************************************* //
CmdMSTDistance::CmdMSTDistance(const Vec3* pointList, int numPoints, float height, float quadraticSplineHeight) :
	Command(CommandType::MST_DISTANCE),
	_height(height),
	_quadraticSplineHeight(quadraticSplineHeight)
{
	_mst = ComputeMST( pointList, numPoints );
}

CmdMSTDistance::~CmdMSTDistance()
{
	delete _mst;
}

float CmdMSTDistance::GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	float result;
	const float maxHeight = sqr(_height + _quadraticSplineHeight);

	float height = maxHeight;
	float fx = x*bufferInfo.PixelSize;
	float fy = y*bufferInfo.PixelSize;
	// Compute minimum distance to the mst for each pixel
	auto it = _mst->GetEdgeIterator();
	while( ++it )
	{
		float r;
		float distance = PointLineDistanceSq( ((PNode*)it->GetSrc())->GetPos(),
								((PNode*)it->GetDst())->GetPos(),
								fx, fy, r );

		height = min(height, _height-(_height-sqrtf(distance)));
	}
		
	// Transform foot of the mountain with quadratic spline
	if( height >= _quadraticSplineHeight )
		result = height;
	else {
		// (height+t)^2/(4*t)
		height += _quadraticSplineHeight;
		height = max( 0.0f, height );
		result = height*height/(4.0f*_quadraticSplineHeight);
	}
	// The points on the mst are 0 so multiplication is not possible.
	// Therefore multiply the inverses.
	result = _height - (_height - result)
		* (1-computeHeight(_mst, fx, fy));	// TODO: The 1- has to be maxSummit-

	return result;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdMSTDistance::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdMSTDistance::GeneratorKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayer(Cmd);
}