// ******************************************************************************** //
// OrHeap.h																			//
// ========																			//
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
// Implementing a priority queue (Fibonacci Heap).									//
//																					//
// HashMap:																			//
//	Insert()					O(1)												//
//	Search()					O() - not supported									//
//	Delete(Key)					O()	- not supported									//
//	Delete(Element)				O(log n)											//
//	DeleteMin(Element)			O(log n)											//
//	Min							O(1)												//
//	DecreaseKey(Element)		O(1)												//
//	GetFirst()					O(1)												//
//	GetLast()					O(1)												//
//	GetNext()					O(1)												//
//	GetPrevious()				O(1)												//
//																					//
// It would be no problem to provide Search and key-Delete with O(n) but a key can	//
// occur multiple times in a heap. Therefore such an operation would be insecurely.	//
// To do some searching you can use the iterator.									//
// ******************************************************************************** //

#pragma once

namespace OrE {
namespace ADT {

	// ******************************************************************************** //
	// One node in the Fibonacci heap
	class HeapNode: public ADTElement
	{
		// Make nodes unchangeable for all users except the heap.
		friend class Heap;

		// Prevent copy constructor and operator = being generated.
		HeapNode(const HeapNode&);
		const HeapNode& operator = (const HeapNode&);
	protected:
		// Linked list of nodes which are siblings
		HeapNode* pLeft;
		HeapNode* pRight;
		// The parent node in the tree structure
		HeapNode* pParent;
		// One arbitrary child. (The other children can be reached through the linked list)
		HeapNode* pChild;
		//int iParam;					// Statistic information to mark nodes (marking = a child of this marked node was cut before)
		int iDegree;					// Number of children

		// Constructor inserting in the structure
		HeapNode(HeapNode* _pSibling, void* _pObj, const uint64_t& _qwKey):
			ADTElement(_pObj, _qwKey), pParent(0), pChild(0), iDegree(0)
		{
			if(_pSibling)
			{
				// Double linked list (ring)
				pLeft = _pSibling;
				pRight = _pSibling->pRight;
				_pSibling->pRight->pLeft = this;
				_pSibling->pRight = this;
			} else pLeft = pRight = this;
		}

		// Resets all pointers of _pNewChild and of other influenced nodes.
		// This operation should be possible for each two node, never mind
		// where they were before.
		void InsertToChildrenList( HeapNode* _pNewChild );

	public:
#ifdef _DEBUG
		// Checks all invariants of a heap node. Call in debug only!
		void CheckNode();
#endif
	};
	typedef HeapNode* HeapNodeP;
	typedef HeapNode const * HeapNodePC;

	// ******************************************************************************** //
	class Heap: public ADT
	{
	private:
		// Prevent copy constructor and operator = being generated.
		Heap(const Heap&);
		const Heap& operator = (const Heap&);

		// Clear Kernel function
		//void RecursiveDelete( HeapNodeP _pCurrent );
	protected:
		HeapNodeP m_pRoot;

		// Merge of a part of a heap into the root list
		void CutChildren(HeapNodeP _pPartition);

		// Ensure that no two roots have the same degree
		void Consolidate();

		// Cuts one element and insert it to the root list. (Similar to meld, but do not affect the siblings of the element)
		void Cut(HeapNodeP _pElement);
	public:
		Heap():m_pRoot(0) {}
		~Heap();

		// Heap operations
		// Delete minimum element and return the data (element is deleted)
		void* DeleteMin();

		// Show the minimum element
		HeapNodeP Min();

		// Change the key value and reorder the elements if necessary.
		// The _qwNewKey can be lower or higher than the old one. (In contrast to the
		// standard fibonacci heap, which allows decreases only).
		void ChangeKey(HeapNodeP _pElement, uint64_t _qwNewKey);

		// general ADT operations
		// Standard operation insert - _qwKey is the priority of the node.
		HeapNodeP Insert(void* _pObject, uint64_t _qwKey);

		// Unsupported function (doing nothing)
		void Delete(uint64_t _qwKey);

		// The only arbitrary delete operation for the heap
		void Delete(ADTElementP _pElement);

		// Remove everything
		void Clear();

		// Unsupported function (returning 0)
		HeapNodeP Search(uint64_t _qwKey);

		// First element = min element
		HeapNodeP GetFirst();
		HeapNodeP GetLast();
		HeapNodeP GetNext(ADTElementP _pCurrent);				// Preorder traverse
		HeapNodeP GetPrevious(ADTElementP _pCurrent);			// Preorder traverse

		typedef OrE::ADT::Iterator<HeapNode> Iterator;
	};

}; // namespace ADT
}; // namespace OrE
// *************************************EOF**************************************** //
