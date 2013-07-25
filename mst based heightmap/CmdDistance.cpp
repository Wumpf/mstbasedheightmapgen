#include "CmdDistance.hpp"

// ************************************************************************* //
// Use a global interpolation to generate a height for a certain point
float computeHeight(const OrE::ADT::Mesh* graph, float x, float y)
{
	// Calculate a weighted sum of all heights where the weight is the radial
	// basis function.
	auto it = graph->GetNodeIterator();
	float height = 0;
	float weightSum = 0;
	while( ++it ) {
		const Vec3& vPos = ((PNode*)&it)->GetPos();
		//float distance = 1.0f/(1.0f + 2.0f*(sqr(vPos.y-y) + sqr(vPos.x-x)));	// Inverse quadric RBF
		float distance = exp(-0.0006f*(sqr(vPos.y-y) + sqr(vPos.x-x)));		// Gaussian
		height += distance * vPos.z;
		weightSum += distance;
	}
	return height * HEIGHT_CODE_FACTOR / weightSum;
}