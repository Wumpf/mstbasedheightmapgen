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



// ******************************************************************************** //
// A wrapper for the MST generation out of a point set
typedef OrE::ADT::Mesh::PosNode PNode;

// Create the minimal spanning tree of a set of points.
OrE::ADT::Mesh* ComputeMST( const Vec3* pointList, int numPoints )
{
	assert( numPoints > 0 );

	// Create a graph with all points fully connected.
	OrE::ADT::Mesh* pGraph = new OrE::ADT::Mesh( numPoints, numPoints*10 );
	
	for( int i=0; i<numPoints; ++i )
	{
		auto node = pGraph->AddNode<PNode>();
		node->SetPos( Vec3(pointList[i].x, pointList[i].y, pointList[i].z/HEIGHT_CODE_FACTOR) );
		// Add edges from this too all other nodes
		int iLastOfLayer = pGraph->GetNumNodes();
		float minLen = 10000000000.0f;
		for( int j=0; j<i; ++j )
		{
			float edgeLen = len( pointList[i] - pointList[j] );
			if( edgeLen < minLen )
			{
				minLen = edgeLen;
				pGraph->AddEdge<OrE::ADT::Mesh::WeightedEdge, PNode>(
						(PNode*)(pGraph->GetNode(j)), node, false );
			}
		}
			
	}
	
	OrE::ADT::Mesh* pMST = pGraph->BuildMST();
		
	delete pGraph;
	return pMST;
}