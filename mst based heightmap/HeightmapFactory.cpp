#include "stdafx.h"
#include "HeightmapFactory.h"
#include "Generator.h"
#include "Filter.h"


HeightmapFactory::HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit) :
	_resolutionX(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_resolutionY(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_worldSizeX(worldSizeX),
	_worldSizeY(worldSizeY),
	_pixelSize(1.0f/heightmapPixelPerWorldUnit),
	_heightThreshold(50.0f),
	_quadraticIncreasePercentage(0.3f),
	_seed( 1111 ),
	_noiseIntensity(1.0f),
	_frequencyHeightDependence(0.5f)
{
	assert(worldSizeX > 0);
	assert(worldSizeY > 0);
	assert(heightmapPixelPerWorldUnit > 0);
}


HeightmapFactory::~HeightmapFactory(void)
{
}

void HeightmapFactory::SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height)
{
	// There is no parameter which doesn't takes an input.
	assert( data );

	switch(type)
	{
	case 0: break;

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
	};
}

unsigned int HeightmapFactory::GetWidth()
{ return _resolutionX; }
	
unsigned int HeightmapFactory::GetHeight()
{ return _resolutionY; }


void HeightmapFactory::Generate(float* dataDestination)
{
	srand( _seed );

/*	for(unsigned int y=0; y<GetHeight(); ++y)
		for(unsigned int x=0; x<GetWidth(); ++x)
			dataDestination[x + y * GetWidth()] = static_cast<float>(rand()) / RAND_MAX;*/

	Vec3* uglyTestBuffer = new Vec3[2000];
	for( int i=0; i<2000; ++i )
	{
		uglyTestBuffer[i].x = (rand()*_worldSizeX/RAND_MAX);// * 0.5f + 0.25f*_worldSizeX;
		uglyTestBuffer[i].y = (rand()*_worldSizeY/RAND_MAX);// * 0.5f + 0.25f*_worldSizeY;
		uglyTestBuffer[i].z = rand()*0.01f/RAND_MAX;
	}

	OrE::ADT::Mesh* mst = ComputeMST( uglyTestBuffer, 200 );

	GenerationDescriptor genDesc( _heightThreshold, _quadraticIncreasePercentage );
	GenerateGraphBased_1( dataDestination, GetWidth(), GetHeight(), _pixelSize, *mst, genDesc );
	delete mst;
	delete[] uglyTestBuffer;

	// Create more natural apeareance
	AddNoise( dataDestination, GetWidth(), GetHeight(), _seed,
		_heightThreshold,
		_frequencyHeightDependence/_heightThreshold,
		_heightThreshold * _noiseIntensity,
		0.01f);

	// Normalize for visual output
	Normalize( dataDestination, GetWidth(), GetHeight() );
}