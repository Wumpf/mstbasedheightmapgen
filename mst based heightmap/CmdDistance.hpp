#pragma once

#include <cstdint>
#include "src-mst/OrADTObjects.h"
#include "src-mst/OrHeap.h"
#include "src-mst/OrHash.h"
#include "src-mst/OrGraph.h"

typedef OrE::ADT::Mesh::PosNode PNode;

/// \brief A scaling factor used to encode the height per node in a MST node.
/// \details On this way the MST topology does not change if the height is
///		edited.
const float HEIGHT_CODE_FACTOR = 256.0f;

/// \brief Use a global interpolation to generate a height for a certain point.
/// \param [in] graph A graph structure with points. The interpolation is done
///		between the points
/// \param [in] x World space x coordinate.
/// \param [in] y World space y coordinate.
/// \return An interpolated height froms the nodes in the graph.
float computeHeight(const OrE::ADT::Mesh* graph, float x, float y);


/// \brief Create the minimal spanning tree of a set of points.
///
OrE::ADT::Mesh* ComputeMST( const Vec3* pointList, int numPoints );