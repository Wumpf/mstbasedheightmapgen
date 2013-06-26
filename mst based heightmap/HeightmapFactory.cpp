#include "stdafx.h"
#include "HeightmapFactory.h"
#include "Generator.h"
#include "Filter.h"


HeightmapFactory::HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit) :
	_resolutionX(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_resolutionY(static_cast<unsigned int>(worldSizeY * heightmapPixelPerWorldUnit)),
	_worldSizeX(worldSizeX),
	_worldSizeY(worldSizeY),
	_pixelSize(1.0f/heightmapPixelPerWorldUnit),
	_heightThreshold(50.0f),
	_quadraticIncreasePercentage(0.3f),
	_seed( 1111 ),
	_noiseIntensity(0.5f),
	_refractionNoiseIntensity(3.0f),
	_frequencyHeightDependence(0.5f),
	_heightDependenceOffset(1.0f),
	_useInverseDistance(true),
	_numSummits(0), _summitList(nullptr)
{
	assert(worldSizeX > 0);
	assert(worldSizeY > 0);
	assert(heightmapPixelPerWorldUnit > 0);
}


HeightmapFactory::~HeightmapFactory(void)
{
	delete[] _summitList;
}

void HeightmapFactory::SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height)
{
	// There is no parameter which doesn't takes an input.
	assert( data );

	switch(type)
	{
	case 0:
		// Set generator type
		assert( width == 1 && height == 1 );
		_useInverseDistance = 1.0f == data[0];
		break;

	case 1:
		// Reset _worldSize
		assert( width == 2 && height == 1 );
		assert( data[0] > 0 && data[1] > 0 );
		_worldSizeX = data[0];
		_worldSizeY = data[1];
		_resolutionX = static_cast<unsigned int>(_worldSizeX / _pixelSize);
		_resolutionY = static_cast<unsigned int>(_worldSizeY / _pixelSize);
		break;

	case 2:
		// Reset resolution
		assert( width == 1 && height == 1 );
		_pixelSize = 1.0f/data[0];
		_resolutionX = static_cast<unsigned int>(_worldSizeX * data[0]);
		_resolutionY = static_cast<unsigned int>(_worldSizeY * data[0]);
		break;

	case 3:
		// Set threshold - otherwise it goes to oo
		assert( width == 1 && height == 1 );
		_heightThreshold = abs(data[0]);
		break;

	case 4:
		// Set how much of the mountain has a quadratic spline.
		assert( width == 1 && height == 1 );
		assert( data[0] >= 0.0f && data[0] <= 1.0f );
		_quadraticIncreasePercentage = data[0];
		break;

	case 5:
		assert( width == 1 && height == 1 );
		_seed = static_cast<int>(data[0]);
		break;

	case 6:
		assert( width == 1 && height == 1 );
		_noiseIntensity = data[0];
		break;

	case 7:
		assert( width == 1 && height == 1 );
		_frequencyHeightDependence = data[0];
		break;

	case 8:
		assert( width == 1 && height == 1 );
		_frequencyGradientDependence = data[0];
		break;

	case 9:
		assert( width==3 );
		_numSummits = height;
		delete[] _summitList;
		_summitList = new Vec3[height];
		for( unsigned int i=0; i<height; ++i )
		{
			_summitList[i].x = data[i*3];
			_summitList[i].y = data[i*3+1];
			_summitList[i].z = data[i*3+2] / HEIGHT_CODE_FACTOR;	// Decrease height to avoid an inluence of the MST structure
		}
		break;

	case 10:
		assert( width == 1 && height == 1 );
		_refractionNoiseIntensity = data[0];
		break;

	case 12:
		assert( width == 1 && height == 1 );
		_heightDependenceOffset = data[0];
		break;

	default:
		// Unimplemented parameter
		assert( false );
	};
}

void HeightmapFactory::GetParameter(unsigned int type, float* outData, unsigned int& outWidth, unsigned int& outHeight)
{
	assert(outData);

	// default:
	outWidth = 1;
	outHeight = 1;

	switch(type)
	{
	case 0:
		outData[0] = _useInverseDistance;
		break;

	case 1:
		// Get _worldSize
		outWidth = 2;
		outData[0] = _worldSizeX;
		outData[1] = _worldSizeY;
		break;

	case 2:
		// Get resolution
		outData[0] = 1.0f / _pixelSize;
		break;

	case 3:
		// Get threshold
		outData[0] = _heightThreshold;
		break;

	case 4:
		// Get how much of the mountain has a quadratic spline.
		outData[0] = _quadraticIncreasePercentage;
		break;

	case 5:
		outData[0] = float(_seed);
		break;

	case 6:
		outData[0] = _noiseIntensity;
		break;

	case 7:
		outData[0] = _frequencyHeightDependence;
		break;

	case 8:
		outData[0] = _frequencyGradientDependence;
		break;

	case 9:
		outWidth = _numSummits;
		for( unsigned int i=0; i<_numSummits; ++i )
		{
			outData[i*3] = _summitList[i].x;
			outData[i*3+1] = _summitList[i].y;
			outData[i*3+2] = _summitList[i].z * HEIGHT_CODE_FACTOR;
		}
		break;

	case 10:
		outData[0] = _refractionNoiseIntensity;
		break;

	case 11:
		// Minimum and maximum height
		outWidth = 2;
		outData[0] = _minHeight;
		outData[1] = _maxHeight;
		break;

	case 12:
		outData[0] = _heightDependenceOffset;
		break;

	default:
		// Unimplemented parameter
		assert( false );
	};
}

unsigned int HeightmapFactory::GetWidth()
{ return _resolutionX; }
	
unsigned int HeightmapFactory::GetHeight()
{ return _resolutionY; }


void HeightmapFactory::Generate(float* dataDestination)
{
	srand( _seed );


/*	Vec3* uglyTestBuffer = new Vec3[2000];
	for( int i=0; i<2000; ++i )
	{
		uglyTestBuffer[i].x = (rand()*_worldSizeX/RAND_MAX);// * 0.5f + 0.25f*_worldSizeX;
		uglyTestBuffer[i].y = (rand()*_worldSizeY/RAND_MAX);// * 0.5f + 0.25f*_worldSizeY;
		uglyTestBuffer[i].z = rand()*0.00390625f/RAND_MAX;
	}*/

	OrE::ADT::Mesh* mst = ComputeMST( _summitList, _numSummits );

	GenerationDescriptor genDesc( _useInverseDistance, _heightThreshold, _quadraticIncreasePercentage );
	GenerateGraphBased_1( dataDestination, GetWidth(), GetHeight(), _pixelSize, *mst, genDesc );
	delete mst;
	//delete[] uglyTestBuffer;

	// Create more natural apeareance by different noises
	if( _refractionNoiseIntensity > 0.0f )
		RefractWithNoise( dataDestination, GetWidth(), GetHeight(), _seed, _refractionNoiseIntensity );

	if( _noiseIntensity > 0.0f )
	{
		float maxGeneratedHeight = (_quadraticIncreasePercentage + 1) * _heightThreshold;
		AddNoise( dataDestination, GetWidth(), GetHeight(), _seed,
			_heightDependenceOffset * maxGeneratedHeight,
			_frequencyHeightDependence / 100,
			_frequencyGradientDependence,
			100 * _noiseIntensity,
			0.01f );
	}

	// Normalize for visual output
	Normalize( dataDestination, GetWidth(), GetHeight(), _minHeight, _maxHeight );
}