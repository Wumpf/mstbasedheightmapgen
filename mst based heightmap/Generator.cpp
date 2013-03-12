#include <stdint.h>
// Stuff copied from OrBaseLib
#include "src-mst/OrADTObjects.h"
#include "src-mst/OrHeap.h"
#include "src-mst/OrHash.h"
#include "src-mst/OrGraph.h"

#include <thread>
#include "Generator.h"

typedef OrE::ADT::Mesh::PosNode PNode;

// Create the minimal spanning tree of a set of points.
OrE::ADT::Mesh* ComputeMST( Vec3* pointList, int numPoints )
{
	assert( numPoints > 0 );

	// Create a graph with all points fully connected.
	OrE::ADT::Mesh* pGraph = new OrE::ADT::Mesh;
	
	for( int i=0; i<numPoints; ++i )
	{
		auto node = pGraph->AddNode<PNode>();
		node->SetPos( pointList[i] );
		// Add edges from this too all other nodes
		int iLastOfLayer = pGraph->GetNumNodes();
		for( int j=0; j<i; ++j )
			pGraph->AddEdge<OrE::ADT::Mesh::WeightedEdge, PNode>(
						(OrE::ADT::Mesh::PosNode*)(pGraph->GetNode(j)), node, false );
			
	}
	
	OrE::ADT::Mesh* pMST = pGraph->BuildMST();
		
	delete pGraph;
	return pMST;
}

void GenerateMSTBased_Kernel_1( float* dataDestination, int width, int y, const OrE::ADT::Mesh* mst )
{
	int yw = y*width;
	for( int x=0; x<width; ++x )
	{
		dataDestination[yw+x] = 1000000000.0f;
		// Compute minimum distance to the mst for each pixel
		auto it = mst->GetEdgeIterator();
		while( ++it )
		{
			float distance = PointLineDistanceSq( ((PNode*)it->GetSrc())->GetPos(),
									((PNode*)it->GetDst())->GetPos(),
									Vec3( x, y, 0.0f ) ); // TODO scale correct
			if( dataDestination[yw+x] > distance )
				dataDestination[yw+x] = distance;
		}

		dataDestination[yw+x] = sqrtf( dataDestination[yw+x] ) * 0.01f;
		dataDestination[yw+x] = 1.0f - dataDestination[yw+x];
	}
}

// Generates the euklidean distance field to the MST.
void GenerateMSTBased_1( float* dataDestination, int width, int height, const OrE::ADT::Mesh& mst )
{
	std::thread* threads[8];
	for( int y=0; y<height; y+=8 )
	{
		int num = min(8,height-y);
	/*	for( int t=0; t<num; ++t )
			GenerateMSTBased_Kernel_1( dataDestination, width, y+t, &mst );//*/
		
		for( int t=0; t<num; ++t )
			threads[t] = new std::thread( GenerateMSTBased_Kernel_1, dataDestination, width, y+t, &mst );
		for( int t=0; t<num; ++t )
		{
			threads[t]->join();
			delete threads[t];
		}//*/
	}
}