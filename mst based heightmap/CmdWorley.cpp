#include <thread>
#include <memory>
#include <cassert>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

// ************************************************************************* //
CmdWorly::CmdWorly(const Vec3* pointList, int numPoints, int nthNeighbor, float height) :
	Command(CommandType::MST_DISTANCE),
	_numPoints(numPoints),
	_nthNeighbor(nthNeighbor),
	_height(height)
{
	assert(numPoints > nthNeighbor);

	_points = new Vec3[numPoints];
	memcpy( _points, pointList, numPoints * sizeof(Vec3) );
}

CmdWorly::~CmdWorly()
{
	delete[] _points;
}

float CmdWorly::GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	float fx = x*bufferInfo.PixelSize;
	float fy = y*bufferInfo.PixelSize;

	// Brute force implementation - search the nth nearest neighbor with a
	// linear search. Remember the first n distances.
	std::unique_ptr<float[]> aDistancesSq(new float[_nthNeighbor+1]);
	for(int i=0; i<=_nthNeighbor; ++i) aDistancesSq[i] = std::numeric_limits<float>::max();

	for(int i=0; i<_numPoints; ++i)
	{
		float fDistanceSq = sqr(fx-_points[i].x) + sqr(fy-_points[i].y) + sqr(_points[i].z);
		// Update minimum list (sorted)
		int j=0;
		while( j<=_nthNeighbor )
		{
			// "Bubble" insertion - toggle elements
			if( fDistanceSq < aDistancesSq[j] )
			{
				float tmp = aDistancesSq[j];
				aDistancesSq[j] = fDistanceSq;
				fDistanceSq = tmp;
			}
			++j;
		}
	}

	return sqrt(aDistancesSq[_nthNeighbor]) * _height;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdWorly::Execute( const MapBufferInfo& bufferInfo,
						const float* prevResult,
						const float* currentResult,
						float* destination)
{
	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdWorly::GeneratorKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayerSeq(Cmd);
}