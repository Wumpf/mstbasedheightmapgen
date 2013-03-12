// ******************************************************************************** //
// OrADTObjects.h																	//
// ==============																	//
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

#pragma once

namespace OrE {
namespace ADT {

// ******************************************************************************** //
// Since Objects are created by the user they have to be destroyed the same way (use callback)
typedef void (*ADTDeleteObjectCallbackP)(void* _pObject);

// ******************************************************************************** //
// Interface for all nodes,... of ADTs
// The ADTs will not make copies of the data. It will just hold the pointer and eventually
// destroy the resource with the callback.
class ADTElement
{
protected:
	int iRef;					// Reference counter: how often was this element inserted?

	// Prevent copy constructor and operator = being generated.
	ADTElement(const ADTElement&);
	const ADTElement& operator = (const ADTElement&);
public:
	uint64_t qwKey;				// 64 Key value
	void* pObject;				// Stored data

	// Constructor: assure consistent state
	// Input: _pObj - A reference to the data or the data itself (32 Bit converted to the pointer)
	//			This isn't touched by any algorithm.
	//		_qwKey - 64 Bit key which can be chosen arbitrary by the user.
	//			The key is used to create an ordering and to find data. It can be
	//			manipulated and sometimes have an other behavior. For details look
	//			at the specified data structure.
	ADTElement(void* _pObj, const uint64_t& _qwKey):iRef(1), qwKey(_qwKey), pObject(_pObj) {}

	virtual ~ADTElement()		{}

	// Reference handling to determine correct point of destruction time
	void AddRef()	{++iRef;}
	int Release()	{return --iRef;}
	int GetRef()	{return iRef;}
};
typedef ADTElement* ADTElementP;

// ******************************************************************************** //
// Interface class for all ADTs of the OrBaseLib
class ADT
{
protected:
	// The callback to delete the data assigned in ADTElement.pObject on
	// Delete and Destroy events.
	ADTDeleteObjectCallbackP m_pDeleteCallback;

	// Save number of contained elements
	int m_iNumElements;

	// Prevent copy constructor and operator = being generated.
	ADT(const ADT&);
	const ADT& operator = (const ADT&);
public:
	ADT():m_pDeleteCallback(0), m_iNumElements(0)	{}				// Creates a consistent ADT object
	virtual ~ADT()						{}
	virtual ADTElementP Insert(void* _pObject, uint64_t _qwKey) = 0;	// Standard operation insert
	virtual void Delete(uint64_t _qwKey) = 0;							// Standard operation delete
	virtual void Delete(ADTElementP _pElement) = 0;					// Sometimes faster operation delete (no search)
	virtual ADTElementP Search(uint64_t _qwKey) = 0;					// Standard search with a key
	virtual void Clear() = 0;										// The remove everything method

	// Navigation on the structure. The order depends on the specified
	// structure but there should always be the possibility to access
	// the whole data by traversing.
	virtual ADTElementP GetFirst() = 0;
	virtual ADTElementP GetLast() = 0;
	virtual ADTElementP GetNext(ADTElementP _pCurrent) = 0;
	virtual ADTElementP GetPrevious(ADTElementP _pCurrent) = 0;

	// Change the delete behavior
	void SetDeleteCallback(ADTDeleteObjectCallbackP _pDeleteCallback)	{m_pDeleteCallback = _pDeleteCallback;}

	// Ask for the fill-state
	int GetNumElements()		{return m_iNumElements;}
};
typedef ADT* ADTP;

// ******************************************************************************** //
// An Iterator is used to traverse over a data set. Each ADT in the OrBaseLib allows
// this kind of access, but sometimes this can be a slow operation.
// Type should be the ADTElement type or a derivate from this.
// Usage example:
//		Iterator<BinaryTreeNode> It(&MyAVLTree);
//		while(--It) or while(++It)
//			std::cout << It->qwKey << "\n";
// The Iterator can be used multiple times in a row. So every time it returns 0 it
// will be in the initialization state afterwards.
// So:	while(++It) doSth()
//		while(++It) doSth2()	is possible without creating a new operator.
// The multi thread behavior is undefined. Instances can be shown or not even if they
// are created/destroyed during the run of the iterator.
// So:	while(++It) MyAVLTree.Add(Sth) can run endless or not depending on the underlying
//		data structure.
template <typename Type> class Iterator
{
private:
	ADTP		m_pADT;					// The initially set ADT (~data set) where we want to traverse.
	ADTElementP	m_pCurrentElement;		// One element of the data set (current access goes to this element)

public:
	// Creates an Iterator for an arbitrary ADT Object and set it to initial
	// empty state. An ++, --, SeekTo... is always necessary to determine the
	// start point of traversing.
	Iterator(ADTP _pADT) {
		m_pADT = _pADT;
		m_pCurrentElement = 0;
	}

	// Jumps to the first or the last element defined by the data structure.
	// Iterator<BinaryTreeNode> It(&MyAVLTree);
	//	It->...				// access would cause an error
	//	It.SeekToBegin();	// ++It and It++ would have the same effect here
	//	It->...				// access now possible
	void SeekToEnd()	{m_pCurrentElement = m_pADT->GetLast();}
	void SeekToBegin()	{m_pCurrentElement = m_pADT->GetFirst();}

	// Override boolean operator that if(Iterator) can be asked
	operator bool () const {return m_pCurrentElement != 0;}

	// Override implicit conversion to the element type
	operator Type* () const {return (Type*)m_pCurrentElement;}

	// Comparison operators
	bool operator==(const Iterator& T) const {return m_pCurrentElement == T.m_pCurrentElement;}
	bool operator!=(const Iterator& T) const {return m_pCurrentElement != T.m_pCurrentElement;}

	// Casting operators
	Type& operator*() const {return *(Type*)m_pCurrentElement;}
	Type* operator->() const {return (Type*)m_pCurrentElement;}
	Type* operator&() const {return (Type*)m_pCurrentElement;}

	// Override ++ and -- to navigate
	Iterator& operator++() {m_pCurrentElement = m_pCurrentElement ? m_pADT->GetNext(m_pCurrentElement) : (m_pADT->GetFirst()); return *this;}
	Iterator& operator--() {m_pCurrentElement = m_pCurrentElement ? m_pADT->GetPrevious(m_pCurrentElement) : (m_pADT->GetLast()); return *this;}
	// Postfix
	const Iterator operator++(int) { Iterator temp = *this; ++*this; return temp; }
	const Iterator operator--(int) { Iterator temp = *this; --*this; return temp; }
};

}; // namespace ADT
}; // namespace OrE
// *************************************EOF**************************************** //
