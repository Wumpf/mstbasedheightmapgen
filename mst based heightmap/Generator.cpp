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

struct BufferDescriptor {
	float* dataDestination;
	int width;
	int height;
	float pixelSize;
};

static void GenerateGraphBased_Kernel_1( BufferDescriptor* bufferDesc, int y, int numLines, const OrE::ADT::Mesh* mst,
						   const GenerationDescriptor& generatorDesc )
{
	for( int i=0; i<numLines; ++i )
	{
		int yw = (y+i)*bufferDesc->width;
		for( int x=0; x<bufferDesc->width; ++x )
		{
			float height = generatorDesc._heightThreshold + generatorDesc._quadraticIncrease;
			height *= height;
			// Compute minimum distance to the mst for each pixel
			auto it = mst->GetEdgeIterator();
			while( ++it )
			{
				float r;
				float distance = PointLineDistanceSq( ((PNode*)it->GetSrc())->GetPos(),
										((PNode*)it->GetDst())->GetPos(),
										x*bufferDesc->pixelSize, (y+i)*bufferDesc->pixelSize, r );
				if( height > distance )
					height = distance;
			}

			// Make it linear (squared distance was searched)
			height = sqrtf( height );

			// Transform linear increase into a decrease of mountain flanks
			height = -height + generatorDesc._heightThreshold;
			// Transform foot of the mountain with quadratic spline
			if( height >= generatorDesc._quadraticIncrease )
				bufferDesc->dataDestination[yw+x] = height;
			else {
				// (height+t)^2/(4*t)
				height += generatorDesc._quadraticIncrease;
				height = max( 0.0f, height );
				bufferDesc->dataDestination[yw+x] = height*height/(4.0f*generatorDesc._quadraticIncrease);
			}
		}
	}
}

// Generates the euklidean distance field to the MST.
void GenerateGraphBased_1( float* dataDestination, int width, int height, float pixelSize, const OrE::ADT::Mesh& mst,
						   const GenerationDescriptor& generatorDesc )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	BufferDescriptor bufferDesc = {dataDestination, width, height, pixelSize};

	int numLinesPerThread = height / 8  +  ( ((height&7)==0) ? 0 : 1 );
	std::thread* threads[8];
	for( int t=0; t<8; ++t )
	{
		int numLines = min(numLinesPerThread, height-t*numLinesPerThread);
		threads[t] = new std::thread( GenerateGraphBased_Kernel_1, &bufferDesc, t*numLinesPerThread, numLines, &mst, generatorDesc );
	}
	for( int t=0; t<8; ++t )
	{
		threads[t]->join();
		delete threads[t];
	}

	// Sequencial test
	// GenerateGraphBased_Kernel_1( &bufferDesc, 0, height, &mst, generatorDesc );
}