// ******************************************************************************** //
// OrHash.cpp																		//
// ==========																		//
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

#include "Stdafx.h"
#include "OrADTObjects.h"
#include "OrHash.h"

// Do not use in the file! This causes endless loops in debug garbage collection.
// #include "../include/OrDebug.h"

#include <stdlib.h>
#include <string.h>
#include <algorithm>
#include <cmath>

using namespace OrE::Algorithm;
using namespace OrE::ADT;

// ******************************************************************************** //
// Create a 32 bit hash value of a data set
uint32_t OrE::Algorithm::CreateHash32(const void* pData, const int iSize)
{
	uint64_t qwHash = CreateHash64(pData, iSize);
	return ((uint32_t)qwHash) ^ ((uint32_t)(qwHash >> 32));
}

// ******************************************************************************** //
// Create a 64 bit hash value of a data set
uint64_t OrE::Algorithm::CreateHash64(const void* pData, const int iSize)
{
	uint64_t qwHash = 0x84222325cbf29ce4ULL;

	uint8_t* pFirst = (uint8_t*)(pData),
		* pLast  = pFirst + iSize;

	while( pFirst < pLast )
	{
		qwHash ^= *pFirst++;
		qwHash *= 0x00000100000001b3ULL;
	}

	return qwHash;
}

// ******************************************************************************** //
// Create a 32 bit hash value of a 0-terminated data set (e.g. strings)
uint32_t OrE::Algorithm::CreateHash32(const void* pData)
{
	return ((uint32_t)CreateHash64(pData)) ^ (((uint32_t)CreateHash64(pData) >> 16) >> 16);
}

// ******************************************************************************** //
// Create a 64 bit hash value of a 0-terminated data set (e.g. strings)
uint64_t OrE::Algorithm::CreateHash64(const void* pData)
{
	uint64_t qwHash = 0x84222325cbf29ce4ULL;

	uint8_t* pFirst = (uint8_t*)(pData);

	while( *pFirst != 0 )	// Termination if byte is zero
	{
		qwHash ^= *pFirst++;
		qwHash *= 0x00000100000001b3ULL;
	}

	return qwHash;
}

// ******************************************************************************** //
// CRC - Error proving hash (cyclic redundancy check)
// Creates the hash with a arbitrary polynomial with a degree less than 32
uint32_t OrE::Algorithm::CreateCRCHash(uint32_t dwPolynom, void* pData, uint32_t dwSize)
{
	uint32_t dwCRC = 0;	// Shift register
	for(uint32_t i=0; i<dwSize; ++i)
	{
		// For each bit
		for(uint32_t j=7; j>=0; --j)
		{
			// Compare first bit of data with current check sum
			if((dwCRC>>31) != (uint32_t)((((uint8_t*)pData)[i]>>j) & 1))
				dwCRC = (dwCRC << 1) ^ dwPolynom;
			else
				dwCRC <<= 1;
		}
	}

	return dwCRC;
}


// ******************************************************************************** //
// Bucket																			//
// ******************************************************************************** //

// Operators for the tree search
bool OrE::ADT::Bucket::IsLess( uint64_t uiKey, const char* _Str ) const
{
	// Standard mode
	if( qwKey < uiKey ) return true;
	// String mode - hash collisions between two different string
	// objects are allowed
	return ( qwKey == uiKey && pcName && _Str
		&& strcmp( pcName, _Str ) < 0 );
}

bool OrE::ADT::Bucket::IsGreater( uint64_t uiKey, const char* _Str ) const
{
	// Standard mode
	if( qwKey > uiKey ) return true;
	// String mode - hash collisions between two different string
	// objects are allowed
	return ( qwKey == uiKey && pcName && _Str
		&& strcmp( pcName, _Str ) > 0 );
}


// ******************************************************************************** //
// Hash map																			//
// ******************************************************************************** //

// ******************************************************************************** //
// The hash function used for names ...
static uint64_t OrStringHash(const char* _pcString)
{
	/*uint32_t dwHash = 5381;

	while(int c = *_pcString++)
		dwHash = ((dwHash << 5) + dwHash) + c; // dwHash * 33 + c
		// Alternative dwHash * 33 ^ c

	return dwHash;//*/
	uint64_t dwHash = 208357;

	while(int c = *_pcString++)
		dwHash = ((dwHash << 5) + (dwHash << 1) + dwHash) ^ c; 

	return dwHash;//*/
	/*uint32_t dwHash = 208357;

	while(int c = *_pcString++)
		dwHash = (dwHash*17) ^ c + ~(dwHash >> 7) + (c >> 3);

	return dwHash;//*/
}

// ******************************************************************************** //
void OrE::ADT::HashMap::RecursiveReAdd(Bucket* _pBucket)
{
	// During resize it is necessary to re-add every thing into the new copy
	if(_pBucket->pObject) Insert(_pBucket->pObject, _pBucket->qwKey);	// If the key contains a char* pointer it is unique and not touched through reinsertion
	if(_pBucket->pLeft) RecursiveReAdd(_pBucket->pLeft);
	if(_pBucket->pRight) RecursiveReAdd(_pBucket->pRight);
}

// ******************************************************************************** //
// Initialization to given start size
OrE::ADT::HashMap::HashMap( uint32_t _dwSize, Mode _Mode ) :
	m_apBuckets( 0 )
{
	// Resize allocates memory too
	Resize( _dwSize );
	m_Mode = _Mode;

#ifdef _DEBUG
	m_dwCollsionCounter = 0;
#endif
}

// ******************************************************************************** //
// Delete data from user and (owned) identifier if in string mode
void OrE::ADT::HashMap::RemoveData(BucketP _pBucket)
{
	if( m_pDeleteCallback && _pBucket->pObject )
		m_pDeleteCallback( _pBucket->pObject );
	free( _pBucket->pcName );
}

// ******************************************************************************** //
// Release all resources
void OrE::ADT::HashMap::RecursiveRelease(BucketP _pBucket)
{
	// Delete user data
	RemoveData(_pBucket);
	// Recursion
	if(_pBucket->pLeft) RecursiveRelease(_pBucket->pLeft);
	if(_pBucket->pRight) RecursiveRelease(_pBucket->pRight);
	// Delete bucket/tree node itself. It is not part of the array.
	delete _pBucket;
}

// ******************************************************************************** //
OrE::ADT::HashMap::~HashMap()
{
	// Delete anything except the map
	Clear();

	// Delete table itself
	free( m_apBuckets );
}

// ******************************************************************************** //
// Remove everything
void OrE::ADT::HashMap::Clear()
{
	// Remove all binary trees and data.
	for(uint32_t i=0;i<m_dwSize;++i)
	{
		// Precondition of RecursiveRelease: Bucket exists (not proven in method
		// for speed up)
		if( m_apBuckets[i] )
			RecursiveRelease(m_apBuckets[i]);
		m_apBuckets[i] = 0;
	}
	m_dwNumElements = 0;
#ifdef _DEBUG
	m_dwCollsionCounter = 0;
#endif
}

// ******************************************************************************** //
// Recreate the table and reinsert all elements
void OrE::ADT::HashMap::Resize(const uint32_t _dwSize)
{
  assert(_dwSize > 0 && "HashMap size of 0 is not permitted!");
	BucketP* pOldList = m_apBuckets;
	uint32_t dwOldSize = m_dwSize;
	// Allocate a new larger? table and make it empty
	m_apBuckets = (BucketP*)malloc(sizeof(BucketP)*_dwSize);
	if(!m_apBuckets) return;	// TODO: report error
	memset(m_apBuckets, 0, sizeof(BucketP)*_dwSize);
	// Set back properties to empty map
	m_dwSize = _dwSize;
	m_dwNumElements = 0;
	
	// This method is used for initialization. In that case the old list does not
	// exists and re insertion is useless.
	if( pOldList )
	{
		// Reinsert all elements
		for(uint32_t i=0;i<dwOldSize;++i)
			if(pOldList[i]) RecursiveReAdd(pOldList[i]);

		// Delete old list
		free(pOldList);
	}
}

// ******************************************************************************** //
// Checks if a resize is required in the current mode
void OrE::ADT::HashMap::TestSize()
{
	// To improve performance extend
	switch( int(m_Mode) & 3 )
	{
		// Slow growth
		case HM_PREFER_SIZE: 
			if(m_dwNumElements >= m_dwSize*3)
				Resize(m_dwSize+3*(uint32_t)sqrt((float)m_dwSize));
			break;
		// Medium growth
		case HM_RESIZE_MODERATE:
			if(m_dwNumElements >= (uint32_t)(m_dwSize*1.5f))
				Resize(m_dwSize+std::max(6*(uint32_t)sqrt((float)m_dwSize), (uint32_t)128));
			break;
		// Fast growth, scarce resize
		case HM_PREFER_PERFORMANCE:
			if(m_dwNumElements >= m_dwSize)
				Resize((uint32_t)((m_dwSize+100)*1.5f));
			break;
	}
}

// ******************************************************************************** //
// If _pBucket points to the initial list this will calculate the index, and -1 if not
int OrE::ADT::HashMap::ListIndex(BucketP _pBucket)
{
	int iIndex = int((BucketP*)_pBucket - m_apBuckets);
	if((iIndex >= (int)m_dwSize) || iIndex < 0) return -1;
	return iIndex;
}

// ******************************************************************************** //
// Standard operation insert
ADTElementP OrE::ADT::HashMap::Insert(void* _pObject, uint64_t _qwKey)
{
	// It seems that during initialization an out-of-memory error occured.
	assert( m_apBuckets );

	TestSize();
	
	// Sort element into binary tree
	uint32_t dwHash = _qwKey%m_dwSize;
	if(m_apBuckets[dwHash])
	{
		BucketP pBucket = m_apBuckets[dwHash];
		while(true)
		{
#ifdef _DEBUG
			++m_dwCollsionCounter;
#endif
			// compare key -> tree search
			if( pBucket->IsGreater( _qwKey ) )
				if(pBucket->pLeft) pBucket = pBucket->pLeft;	// traverse
				else {pBucket->pLeft = new Bucket(_pObject, _qwKey, pBucket); ++m_dwNumElements; return pBucket->pLeft;}
			else if( pBucket->IsLess( _qwKey ) )
				if(pBucket->pRight) pBucket = pBucket->pRight;	// traverse
				else {pBucket->pRight = new Bucket(_pObject, _qwKey, pBucket); ++m_dwNumElements; return pBucket->pRight;}
			else {
				// This data already exists. It is obvious that this element collides with itself.
#ifdef _DEBUG
				--m_dwCollsionCounter;
#endif
				pBucket->AddRef();
				return pBucket;
			}
		}
	} else
	{
		m_apBuckets[dwHash] = new Bucket(_pObject, _qwKey, BucketP(m_apBuckets+dwHash));
		++m_dwNumElements;
		return m_apBuckets[dwHash];
	}
}

// ******************************************************************************** //
// Standard operation delete
void OrE::ADT::HashMap::Delete(uint64_t _qwKey)
{
	Delete(Search(_qwKey));
}

// ******************************************************************************** //
// Faster operation delete (no search)
void OrE::ADT::HashMap::Delete( ADTElementP _pElement )
{
	if(!_pElement) return;
	BucketP pElement = BucketP(_pElement);
	// Are there more references?
	if( pElement->Release() > 0 ) return;

	// Repair tree
	// If _pElement has children search for a replacement of the _pElement node.
	BucketP pReplacement = 0;
	if( pElement->pLeft )
	{
		pReplacement = pElement->pLeft;
		// There is a left subtree - use maximum, it preserves tree properties
		BucketP pBuck = pElement->pLeft->GetLargestChild();
		// Attach right subtree at the left one (all elements larger than in the chosen node)
		pBuck->pRight = pElement->pRight;
		if( pBuck->pRight )
			pBuck->pRight->pParent = pBuck;
		// Remove reference from old node and set to left tree instead
		pElement->pLeft->pParent = pElement->pParent;
	} else if( pElement->pRight )
	{
		pReplacement = pElement->pRight;
		// There is not left tree which can be attached to the right one.
	
		// Remove reference from old node and set to right tree instead
		pElement->pRight->pParent = pElement->pParent;
	} // else: No subtrees needs to be realigned.

	// Update a parent or the real list entry.
	int iI;
	if( (iI = ListIndex(pElement->pParent)) == -1)
	{
		if(pElement->pParent->pLeft == pElement)
			pElement->pParent->pLeft = pReplacement;
		else pElement->pParent->pRight = pReplacement;
	} else m_apBuckets[iI] = pReplacement;
	
	// Delete data
	RemoveData(pElement);
	delete pElement;
	// Now it is removed
	--m_dwNumElements;
}

// ******************************************************************************** //
// Standard search with a key
ADTElementP OrE::ADT::HashMap::Search( uint64_t _qwKey )
{
	// Find correct bucked with hashing
	uint32_t dwHash = _qwKey%m_dwSize;
	BucketP pBucket = m_apBuckets[dwHash];
	// Tree search
	while(pBucket)
	{
		if( pBucket->IsGreater( _qwKey ) ) pBucket = pBucket->pLeft;
		else if( pBucket->IsLess( _qwKey ) ) pBucket = pBucket->pRight;
		else return pBucket;
	}
	return 0;
}

const ADTElement* OrE::ADT::HashMap::Search( uint64_t _qwKey ) const
{
	// Find correct bucked with hashing
	uint32_t dwHash = _qwKey%m_dwSize;
	BucketP pBucket = m_apBuckets[dwHash];
	// Tree search
	while(pBucket)
	{
		if( pBucket->IsGreater( _qwKey ) ) pBucket = pBucket->pLeft;
		else if( pBucket->IsLess( _qwKey ) ) pBucket = pBucket->pRight;
		else return pBucket;
	}
	return 0;
}

// ******************************************************************************** //
// String-Mode functions
// insert using strings
ADTElementP OrE::ADT::HashMap::Insert( void* _pObject, const char* _pcKey )
{
	// It seems that during initialization an out-of-memory error occured.
	assert( m_apBuckets );

	assert( _pcKey );

	TestSize();
	
	// Insert new element now
	uint64_t uiKey = OrStringHash(_pcKey);
	uint32_t dwHash = uiKey%m_dwSize;
	BucketP pBucket = m_apBuckets[dwHash];
	if( pBucket )
	{
		bool bAdded = false;
		while(!bAdded)
		{
#ifdef _DEBUG
			++m_dwCollsionCounter;
#endif
			// Compare key and strings -> tree search
			// It could be that the map contains elements without a string.
			if( pBucket->IsGreater( uiKey, _pcKey ) )
				if(pBucket->pLeft) pBucket = pBucket->pLeft;	// traverse
				else {pBucket->pLeft = new Bucket(_pObject, uiKey, pBucket); bAdded = true;}
			else if( pBucket->IsLess( uiKey, _pcKey ) )
				if(pBucket->pRight) pBucket = pBucket->pRight;	// traverse
				else {pBucket->pRight = new Bucket(_pObject, uiKey, pBucket); bAdded = true;}
			else {
				// This data already exists. It is obvious that this element collides with itself.
#ifdef _DEBUG
				--m_dwCollsionCounter;
#endif
				pBucket->AddRef();
				return pBucket;
			}
		}
		// The loop terminates iff pBucket points to the new node.
	} else
	{
		// Empty bucket
		pBucket = m_apBuckets[dwHash] = new Bucket(_pObject, uiKey, BucketP(m_apBuckets+dwHash));
	}

	// Copy string.
	int iLen = (int)strlen(_pcKey)+1;
	pBucket->pcName = (char*)malloc(iLen);
	memcpy(pBucket->pcName, _pcKey, iLen);
	++m_dwNumElements;
	return pBucket;
}

// ******************************************************************************** //
// Standard operation delete for strings
void OrE::ADT::HashMap::Delete( const char* _pcKey )
{
	assert( _pcKey );
	Delete( Search( _pcKey ) );
}

// ******************************************************************************** //
// search using strings
ADTElementP OrE::ADT::HashMap::Search( const char* _pcKey )
{
	assert( _pcKey );

	// Find bucket with hashing
	uint64_t uiKey = OrStringHash(_pcKey);
	uint32_t dwHash = uiKey%m_dwSize;
	BucketP pBucket = m_apBuckets[dwHash];
	// tree search with strings
	while(pBucket)
	{
		if( pBucket->IsGreater( uiKey, _pcKey ) ) pBucket = pBucket->pLeft;
		else if( pBucket->IsLess( uiKey, _pcKey ) ) pBucket = pBucket->pRight;
		else return pBucket;
	}
	return 0;
}

// ******************************************************************************** //
ADTElementP OrE::ADT::HashMap::GetFirst()
{
	for(uint32_t i=0;i<m_dwSize;++i)
		// Skip all empty buckets.
		if(m_apBuckets[i])
		{
			// Goto the smallest element in the bucket
			return m_apBuckets[i]->GetSmallestChild();
		}

	return 0;
}

// ******************************************************************************** //
ADTElementP OrE::ADT::HashMap::GetLast()
{
	for(uint32_t i=m_dwSize-1;i>=0;--i)
		// Skip all empty buckets.
		if(m_apBuckets[i])
		{
			// Goto the largest element in the bucket
			return m_apBuckets[i]->GetLargestChild();
		}

	return 0;
}

// ******************************************************************************** //
ADTElementP OrE::ADT::HashMap::GetNext(ADTElementP _pCurrent)
{
	BucketP pBuck = (BucketP)_pCurrent;
	// Inorder traverse -> left site was seen before
	if(pBuck->pRight) 
	{
		return pBuck->pRight->GetSmallestChild();
	} else {
		// With no right child we have to move to the parent. We could have seen this,
		// if we are in the right branch now. Then we have to move much more upwards
		// until we come back from a left branch.
		int iIndex = ListIndex(pBuck->pParent);
		while(pBuck->pParent->pRight == pBuck && iIndex==-1)
		{
			pBuck = pBuck->pParent;
			iIndex = ListIndex(pBuck->pParent);
		}
		if(iIndex!=-1)
		{
			do
				if( ++iIndex == (int)m_dwSize ) return 0;	// Reached the end - no next element
			while( !m_apBuckets[iIndex] );
			// Smallest element in bucket
			return m_apBuckets[iIndex]->GetSmallestChild();
		} else
			return pBuck->pParent;
	}
}

// ******************************************************************************** //
ADTElementP OrE::ADT::HashMap::GetPrevious(ADTElementP _pCurrent)
{
	BucketP pBuck = (BucketP)_pCurrent;
	// Inorder traverse -> left site was seen before
	if(pBuck->pLeft) 
	{
		return pBuck->pLeft->GetLargestChild();
	} else {
		// With no left child we have to move to the parent. We could have seen this,
		// if we are in the left branch now. Then we have to move much more upwards
		// until we come back from a right branch.
		int iIndex = ListIndex(pBuck->pParent);
		while(pBuck->pParent->pLeft == pBuck && iIndex==-1) 
		{
			pBuck = pBuck->pParent;
			iIndex = ListIndex(pBuck->pParent);
		}
		if(iIndex != -1)
		{
			do
				if( --iIndex == -1 ) return 0;	// Reached the end - no next element
			while( !m_apBuckets[iIndex] );
			// Largest element in bucket
			return m_apBuckets[iIndex]->GetLargestChild();
		} else
			return pBuck->pParent;
	}
}

// *************************************EOF**************************************** //
