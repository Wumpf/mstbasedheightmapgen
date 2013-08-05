#include <thread>
#include <memory>
#include "CommandInfo.h"
#include "math.hpp"

using namespace std::placeholders;

// ************************************************************************* //
CmdVoronoi::CmdVoronoi(const Vec3* pointList, int numPoints, float height) :
	Command(CommandType::MST_DISTANCE),
	_numPoints(numPoints),
	_height(height)
{
	_points = new Vec3[numPoints];
	memcpy( _points, pointList, numPoints * sizeof(Vec3) );
}

CmdVoronoi::~CmdVoronoi()
{
	delete[] _points;
}

float CmdVoronoi::GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult )
{
	float fx = x*bufferInfo.PixelSize;
	float fy = y*bufferInfo.PixelSize;

	// Brute force implementation - search the nth nearest neighbor with a
	// linear search.
	float fMinDistanceSq = std::numeric_limits<float>::max();

	for(int i=0; i<_numPoints; ++i)
	{
		float fDistanceSq = sqrt(sqr(fx-_points[i].x) + sqr(fy-_points[i].y)) - _points[i].z;
		// Update minimum
		if( fDistanceSq < fMinDistanceSq )
			fMinDistanceSq = fDistanceSq;
	}

	return _height - (fMinDistanceSq) * _height;
}

// ************************************************************************* //
// This commando creates a value noise which may depend on the previous step.
void CmdVoronoi::Execute( const MapBufferInfo& bufferInfo,
						const float* prevResult,
						const float* currentResult,
						float* destination)
{
	// **** Per pixel **** //
	CommandDesc Cmd(bufferInfo, prevResult, currentResult,
		std::bind(&CmdVoronoi::GeneratorKernel, this, _1, _2, _3, _4, _5),
		destination);

	GenerateLayer(Cmd);
}