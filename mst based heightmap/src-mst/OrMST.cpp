// ******************************************************************************** //
// OrMST.cpp																		//
// =========																		//
// This file is part of the OrBaseLib.												//
//																					//
// Author: Johannes Jendersie														//
//																					//
// Here is a quite easy licensing as open source:									//
// http://creativecommons.org/licenses/by/3.0/										//
// If you use parts of this project, please let me know what the purpose of your	//
// project is. You can do this by writing a comment at github.com/Jojendersie/.		//
//																					//
// For details on this project see: Readme.txt										//
// ******************************************************************************** //
// This is the implementation of Prim's MST algorithm.								//
// The runtime is O(e+n log n). The implementation is done with a fibonacci heap.	//
// ******************************************************************************** //

#include "Stdafx.h"
#include <limits.h>
#include <math.h>
#include <stdint.h>

#include "OrADTObjects.h"
#include "OrHash.h"
#include "OrGraph.h"
#include "OrHeap.h"

// ******************************************************************************** //
struct PrimNodeInfo
{
	// Minimal distance to the MST (0 if part of it)
	float fDist;

	// The parent element. This points to the one in the original graph!
	OrE::ADT::Mesh::PosNodeP pParent;

	// Corresponding node in the new graph!
	OrE::ADT::Mesh::PosNodeP pNode;

	// The node in the heap to change the priority fast.
	OrE::ADT::HeapNodeP pHeapNode;
	
	PrimNodeInfo() :
		fDist( std::numeric_limits<float>::infinity() ),
		pParent( 0 )	{}
};

// ******************************************************************************** //
static void CopyNodes( const OrE::ADT::Mesh& _Src, OrE::ADT::Mesh& _Dst, PrimNodeInfo* _pInfoDst )
{
	OrE::ADT::Graph::NodeIterator It = _Src.GetNodeIterator();
	uint32_t i = 0;
	while( ++It )
	{
		// Set Label in original graph
		OrE::ADT::Mesh::PosNodeP(&It)->m_pTmpLabel = &_pInfoDst[i];

		_pInfoDst[i].pNode = OrE::ADT::Mesh::PosNodeP( It->CopyTo( _Dst ) );
		++i;
	}
}

// ******************************************************************************** //
static void FillHeap( const OrE::ADT::Mesh& _Src, OrE::ADT::Heap& _Dst, PrimNodeInfo* _pInfoDst )
{
	// Initialization depends on number only. Each node is not part of any tree.
	OrE::ADT::Graph::NodeIterator It = _Src.GetNodeIterator();
	uint32_t i = 0;
	while( ++It )
	{
		// Correspondence should not be changed since CopyNode. Check for multithreading
		// caused node insertions/deletions.
		assert( OrE::ADT::Mesh::PosNodeP(&It)->m_pTmpLabel == &_pInfoDst[i] );
		// Heap
		_pInfoDst[i].pHeapNode = _Dst.Insert( &It, std::numeric_limits<uint64_t>::max() );
		++i;
	}
}

// ******************************************************************************** //
// Compute the minimal spanning tree for this graph. The result is written to a copy.
OrE::ADT::Mesh* OrE::ADT::Mesh::BuildMST() const
{
	// Initialize the distances for all nodes and create a forest in an
	// index structure. (For fast random access; fast heap node access)
	PrimNodeInfo* aInfos = new PrimNodeInfo[GetNumNodes()];

	// Create the new graph as copy
	Mesh* M = new Mesh;
	CopyNodes( *this, *M, aInfos );
	
	// Fill the Fibonacci heap with all nodes whether they are connected or not.
	Heap FibHeap;
	FillHeap( *this, FibHeap, aInfos );

	// For all nodes
	while( FibHeap.Min() )
	{
		PosNodeP pPivot = reinterpret_cast<PosNodeP>(FibHeap.DeleteMin());
		// If the node has infinite distance it is not part of any tree -> create
		// a new one with only this node. This happens implicit because it has no
		// parent, but updates all neighbours.
		PrimNodeInfo* pPivotInfo = (PrimNodeInfo*)pPivot->m_pTmpLabel;
		pPivotInfo->fDist = 0.0f;
		// Update all neighbour points
		OrE::ADT::Graph::Node::OutEdgeIterator AOutIt = pPivot->GetOutEdgeIterator();
		while( ++AOutIt )
		{
			float fDist = WeightedEdgeP( &AOutIt )->GetWeight();
			PosNodeP pNeighbourNode = PosNodeP( WeightedEdgeP( &AOutIt )->GetOther(pPivot) );
			PrimNodeInfo* pNeighbour = ((PrimNodeInfo*)pNeighbourNode->m_pTmpLabel);
			if( pNeighbour->fDist > fDist )
			{
				// This edge is shorter than the one used before.
				pNeighbour->fDist = fDist;
				pNeighbour->pParent = pPivot;
				FibHeap.ChangeKey( pNeighbour->pHeapNode, uint64_t( fDist * 1000000.0 ) );
			}
		}

		// Create a new edge between parent and the new node in the Graph copy.
		if( pPivotInfo->pParent )
			M->AddEdge< WeightedEdge, PosNode >(
				((PrimNodeInfo*)pPivotInfo->pParent->m_pTmpLabel)->pNode,
				pPivotInfo->pNode,
				pPivotInfo->pParent->GetEdge( pPivot )->IsDirected()
			);
	}

	delete[] aInfos;
	return M;
}


// ******************************************************************************** //
// A wrapper for the MST generation out of a point set
typedef OrE::ADT::Mesh::PosNode PNode;

// Create the minimal spanning tree of a set of points.
OrE::ADT::Mesh* OrE::ADT::Mesh::ComputeMST( const Vec3* pointList, int numPoints )
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

// ******************************************************************************** //
