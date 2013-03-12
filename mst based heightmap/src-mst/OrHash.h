// ******************************************************************************** //
// OrHash.h																			//
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
//																					//
// ******************************************************************************** //
// Implementing some hash functions and a hash map.									//
//																					//
// HashMap ( HM_PREFER_PERFORMANCE ):												//
//	Insert()					O(1) amort.											//
//	Search()					O(1) amort.											//
//	Delete(Key)					O(1) amort.											//
//	Delete(Element)				O(1) amort.											//
//	GetFirst()					O(1) amort.											//
//	GetLast()					O(1) amort.											//
//	GetNext()					O(1) amort.											//
//	GetPrevious()				O(1) amort.											//
//	In other modes than HM_PREFER_PERFORMANCE only the speed of Insert() differ		//
//	in asymptoticly view. Insert()	O(sqrt(n)) amort.								//
//	All other methods get slightly bigger constant factors.							//
//																					//
// HashMap - String-Mode															//
// In the string mode the objects can added with strings as keys. These strings are	//
// copied internal and the reference is saved in the higher 32 Bit part of the key.	//
// It is still possible to add objects with other keys to the same map, but they	//
// can only use the lower 32 Bit part.												//
// ******************************************************************************** //

#pragma once

namespace OrE {
namespace Algorithm {

// ******************************************************************************** //
// Standard hash for any data. Should result in nice uniform distributions mostly.
uint32_t CreateHash32(const void* pData, const int iSize);	// Create a 32 bit hash value of a data set
uint64_t CreateHash64(const void* pData, const int iSize);	// Create a 64 bit hash value of a data set
uint32_t CreateHash32(const void* pData);					// Create a 32 bit hash value of a 0-terminated data set (e.g. strings)
uint64_t CreateHash64(const void* pData);					// Create a 64 bit hash value of a 0-terminated data set (e.g. strings)

// ******************************************************************************** //
// CRC - Error proving hash (cyclic redundancy check)
// The CRC functions determine the remaining of polynomial division.
// This value can be appended to the data. To prove correctness of the data repeat
// the CRC function with the same polynomial. A result of 0 denotes, that no checkable
// error occurred.
// Creates the hash with a arbitrary polynomial with a degree less than 32
uint32_t CreateCRCHash(uint32_t dwPolynom, void* pData, uint32_t dwSize);

// Using of polynomial: x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x + 1
inline uint32_t CreateCRC32IEEEHash(void* pData, uint32_t dwSize)		{return CreateCRCHash(0x04C11DB7, pData, dwSize);}

// Using of polynomial: x24 + x23 + x18 + x17 + x14 + x11 + x10 + x7 + x6 + x5 + x4 + x3 + x + 1
inline uint32_t CreateCRC24RadixHash(void* pData, uint32_t dwSize)	{return CreateCRCHash(0x864CFB, pData, dwSize);}

// Using of polynomial: x16+x12+x5+1
inline uint32_t CreateCRCCCITT16Hash(void* pData, uint32_t dwSize)	{return CreateCRCHash(0x00011022, pData, dwSize);}

// Using of polynomial: x16+x15+x2+1
inline uint32_t CreateCRC16Hash(void* pData, uint32_t dwSize)			{return CreateCRCHash(0x00018006, pData, dwSize);}

}; // namespace Algorithm
namespace ADT {


// ******************************************************************************** //
// The buckets are a very simple binary trees without any optimization.
class Bucket: public ADTElement
{
	Bucket(void* _pObj, const uint64_t& _qwKey, Bucket* _pParent) :
			ADTElement(_pObj, _qwKey),
			pLeft(0),
			pRight(0),
			pParent(_pParent),
			pcName(0) {}
private:
	Bucket* pLeft;
	Bucket* pRight;
	Bucket* pParent;

	// The name of the element is only used in string mode.
	char* pcName;

	// Return the right/left end of a branch. This can be 'this' too.
	inline Bucket* GetLargestChild()	{ Bucket* pRes = this; while(pRes->pRight) pRes = pRes->pRight; return pRes; }
	inline Bucket* GetSmallestChild()	{ Bucket* pRes = this; while(pRes->pLeft) pRes = pRes->pLeft; return pRes; }

	// Operators for the tree search
	bool IsLess( uint64_t uiKey, const char* _Str = 0 ) const;

	bool IsGreater( uint64_t uiKey, const char* _Str = 0 ) const;

	friend class HashMap;

	// Prevent copy constructor and operator = being generated.
	Bucket(const Bucket&);
	const Bucket& operator = (const Bucket&);
};
typedef Bucket* BucketP;

// ******************************************************************************** //
// The hash map is a structure to store and find data in nearly constant time (stochastically).
class HashMap: public ADT
{
public:
	enum Mode
	{
		HM_NO_RESIZE = 0,						// Constant size (no reszie during insertion); high collision count; could be verry fast for some purposes
		HM_PREFER_SIZE = 1,						// Resize if {#E>=3*Size} to {Size+3*sqrt(Size)}
		HM_RESIZE_MODERATE = 2,					// Resize if {#E>=1.5*Size} to {Size+Max(128,6*sqrt(Size))}
		HM_PREFER_PERFORMANCE = 3,				// Resize if {#E>=Size} to {(Size+100)*1.5}
	};

private:
	BucketP*		m_apBuckets;				// An array with buckets (binary trees)
	uint32_t			m_dwSize;					// Size of the array and therewith of hash map
	uint32_t			m_dwNumElements;			// Number of elements currently in map (can be larger than array size)
	Mode			m_Mode;						// Modes set in initialization (String mode?, Resize mode?)

	void RemoveData(BucketP _pBucket);
	void RecursiveReAdd(BucketP _pBucket);
	void RecursiveRelease(BucketP _pBucket);
	void TestSize();							// Checks if a resize is required in the current mode
	int ListIndex(BucketP _pBucket);			// If _pBucket points to the initial list this will calculate the index, and -1 if not

	// Prevent copy constructor and operator = being generated.
	HashMap(const HashMap&);
	HashMap& operator = (const HashMap&);
public:

	HashMap(uint32_t _dwSize, Mode _Mode);
	virtual ~HashMap();

	// Remove everything
	void Clear();

	// Recreate the table and reinsert all elements
	void Resize(const uint32_t _dwSize);

	// Standard operation insert; If already existing, the object is NOT overwritten,
	// but reference counter is increased.
	ADTElementP Insert(void* _pObject, uint64_t _qwKey);

	// Standard operation delete
	void Delete(uint64_t _qwKey);

	// Faster operation delete (no search)
	void Delete(ADTElementP _pElement);

	// Standard search with a key
	ADTElementP Search(uint64_t _qwKey);
	const ADTElement* Search(uint64_t _qwKey) const;

	// String-Mode functions
	// insert using strings; If already existing, the object is NOT overwritten,
	// but reference counter is increased
	ADTElementP Insert(void* _pObject, const char* _pcKey);

	// Standard operation delete for strings
	void Delete(const char* _pcKey);

	// search using strings
	ADTElementP Search(const char* _pcKey);

	// The overall first object from the first non-empty bucket
	ADTElementP GetFirst();
	ADTElementP GetLast();
	ADTElementP GetNext(ADTElementP _pCurrent);
	ADTElementP GetPrevious(ADTElementP _pCurrent);

	bool IsEmpty() const			{return m_dwNumElements==0;}

	// Objects/elements in the hash map. Not its capacity.
	uint32_t GetNumElements() const	{return m_dwNumElements;}

	// Size of table array. This is not the number of elements (GetNumElements).
	uint32_t GetSize() const			{return m_dwSize;}

	typedef Iterator<ADTElement> Iterator;

#ifdef _DEBUG
	uint32_t m_dwCollsionCounter;
#endif
};
typedef HashMap* HashMapP;

}; // namespace ADT
}; // namespace OrE
// *************************************EOF**************************************** //
