#pragma once

#include "math.hpp"

/// \brief Create the minimal spanning tree of a set of points.
/// \details The points are connected to each other (fully connnected graph)
///		and the MST of the resulting graph is computed.
/// \return The mst as graph with positions as nodes and the distance saved
///		in the edges. Must be deleted after use.
OrE::ADT::Mesh* ComputeMST( Vec3* pointList, int numPoints );


struct GenerationDescriptor {
	float _heightThreshold;	/// <<\brief Cuts the distance function at a certain level.
	float _quadraticIncrease;

	GenerationDescriptor(float heightThreshold, float quadraticIncreasePercentage) :
		_heightThreshold(heightThreshold)
	{
		// Compute spline factors for the quadraticIncrease percentage.
		// The spline is: ax^2
		// with a = 1/(4*heightWithSpline)
		_quadraticIncrease = abs(heightThreshold) * quadraticIncreasePercentage;
	}
};

/// \brief Generates the euklidean distance field to the MST.
/// \param [out] dataDestination Destination buffer for the highmap with a size
///		of width*height*sizeof(float)
/// \param [in] width Number of pixels in X direction.
/// \param [in] width Number of pixels in Y direction.
/// \param [in] pixelSize Sampling distance during generation.
/// \param [in] graph An minimal spanning tree or any other graph used as
///		mountain crest.
void GenerateGraphBased_1( float* dataDestination, int width, int height,
						   float pixelSize,
						   const OrE::ADT::Mesh& graph,
						   const GenerationDescriptor& generatorDesc );