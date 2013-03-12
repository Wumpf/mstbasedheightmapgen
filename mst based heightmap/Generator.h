#pragma once

#include "math.hpp"

/// \brief Create the minimal spanning tree of a set of points.
/// \details The points are connected to each other (fully connnected graph)
///		and the MST of the resulting graph is computed.
/// \return The mst as graph with positions as nodes and the distance saved
///		in the edges. Must be deleted after use.
OrE::ADT::Mesh* ComputeMST( Vec3* pointList, int numPoints );

/// \brief Generates the euklidean distance field to the MST.
void GenerateMSTBased_1( float* dataDestination, int width, int height, const OrE::ADT::Mesh& mst );