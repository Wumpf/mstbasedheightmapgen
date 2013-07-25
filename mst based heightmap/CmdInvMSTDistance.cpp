#include <thread>
#include "CommandInfo.h"
#include "math.hpp"
#include "CmdDistance.hpp"

using namespace std::placeholders;



// ************************************************************************* //
CmdInvMSTDistance::CmdInvMSTDistance(const Vec3* pointList, int numPoints, float height, float quadraticSplineHeight) :
	Command(CommandType::MST_INV_DISTANCE),
	_height(height),
	_quadraticSplineHeight(quadraticSplineHeight)
{
	_mst = ComputeMST( pointList, numPoints );
}

CmdInvMSTDistance::~CmdInvMSTDistance()
{
	delete _mst;
}


float CmdInvMSTDistance::GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	const float maxHeight = _height + _quadraticSplineHeight;
	const float py = y*bufferInfo.PixelSize;

	float height = -_quadraticSplineHeight;
	// Compute minimum distance to the mst for each pixel
	auto it = _mst->GetEdgeIterator();
	while( ++it )
	{
		float r;
		const Vec3& vP0 = ((PNode*)it->GetSrc())->GetPos();
		const Vec3& vP1 = ((PNode*)it->GetDst())->GetPos();
		float distance = PointLineDistanceSq( vP0,
								vP1,
								x*bufferInfo.PixelSize, py, r );

		float unparametrizedHeight = maxHeight-sqrtf(distance);
		// (height+t)^2/(4*t)
		if( unparametrizedHeight < _quadraticSplineHeight )
		{
			unparametrizedHeight += _quadraticSplineHeight;
			unparametrizedHeight = max( 0.0f, unparametrizedHeight );
			unparametrizedHeight = sqr(unparametrizedHeight)/(4.0f*_quadraticSplineHeight);
		}
		height = max( unparametrizedHeight, height );
	}

	return height * computeHeight(_mst, x*bufferInfo.PixelSize, py) / _height;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdInvMSTDistance::Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination)
{
	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdInvMSTDistance::GeneratorKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayer(Cmd);
}