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
	OrE::ADT::Mesh* pGraph = new OrE::ADT::Mesh( numPoints, numPoints*10 );
	
	for( int i=0; i<numPoints; ++i )
	{
		auto node = pGraph->AddNode<PNode>();
		node->SetPos( pointList[i] );
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


static void GenerateGraphBased_Kernel_1( float* dataDestination, int width, int y, float pixelSize, const OrE::ADT::Mesh* mst )
{
	for( int i=0; i<4; ++i )
	{
		int yw = (y+i)*width;
		for( int x=0; x<width; ++x )
		{
			dataDestination[yw+x] = 1000000000.0f;
			// Compute minimum distance to the mst for each pixel
			auto it = mst->GetEdgeIterator();
			while( ++it )
			{
				float distance = PointLineDistanceSq( ((PNode*)it->GetSrc())->GetPos(),
										((PNode*)it->GetDst())->GetPos(),
										x*pixelSize, (y+i)*pixelSize );
										//Vec3( x*pixelSize, (y+i)*pixelSize, 0.0f ) );
				if( dataDestination[yw+x] > distance )
					dataDestination[yw+x] = distance;
			}

			dataDestination[yw+x] = sqrtf( dataDestination[yw+x] ) * 0.01f;
		//	float inverse = 1.0f - dataDestination[yw+x];
		//	dataDestination[yw+x] = dataDestination[yw+x]*dataDestination[yw+x] + inverse*inverse;
			dataDestination[yw+x] = 1.0f - dataDestination[yw+x];
		}
	}
}

// Generates the euklidean distance field to the MST.
void GenerateGraphBased_1( float* dataDestination, int width, int height, float pixelSize, const OrE::ADT::Mesh& mst )
{
	// Hight must be devisible through 4
	assert( height & 3 == 0 );

	//int numLinesPerThread = height / 8  +  ( height&7==0 ? 0 : 1 );
	std::thread* threads[8];
	for( int y=0; y<height; y+=32 )
	{
		int num = min(8,(height-y)/4);
	/*	for( int t=0; t<num; ++t )
			GenerateMSTBased_Kernel_1( dataDestination, width, y+t, &mst );//*/
		
		for( int t=0; t<num; ++t )
			threads[t] = new std::thread( GenerateGraphBased_Kernel_1, dataDestination, width, y+t*4, pixelSize, &mst );
		for( int t=0; t<num; ++t )
		{
			threads[t]->join();
			delete threads[t];
		}//*/
	}
}