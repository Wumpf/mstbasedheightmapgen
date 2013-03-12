#include "stdafx.h"
#include "HeightmapFactory.h"
#include "Generator.h"
#include "Filter.h"


HeightmapFactory::HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit) :
	_resolutionX(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_resolutionY(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_worldSizeX(worldSizeX),
	_worldSizeY(worldSizeY),
	_pixelSize(1.0f/heightmapPixelPerWorldUnit)
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
	// TODO
}

unsigned int HeightmapFactory::GetWidth()
{ return _resolutionX; }
	
unsigned int HeightmapFactory::GetHeight()
{ return _resolutionY; }


void HeightmapFactory::Generate(float* dataDestination)
{
/*	for(unsigned int y=0; y<GetHeight(); ++y)
		for(unsigned int x=0; x<GetWidth(); ++x)
			dataDestination[x + y * GetWidth()] = static_cast<float>(rand()) / RAND_MAX;*/

	Vec3* uglyTestBuffer = new Vec3[2000];
	for( int i=0; i<2000; ++i )
	{
		uglyTestBuffer[i].x = rand()*_worldSizeX/RAND_MAX;
		uglyTestBuffer[i].y = rand()*_worldSizeY/RAND_MAX;
		uglyTestBuffer[i].z = 0.0f;//rand()*100.0f/RAND_MAX;
	}

	OrE::ADT::Mesh* mst = ComputeMST( uglyTestBuffer, 2000 );
	GenerateGraphBased_1( dataDestination, GetWidth(), GetHeight(), _pixelSize, *mst );
	delete mst;
	delete[] uglyTestBuffer;

	// Normalize for visual output
	Normalize( dataDestination, GetWidth(), GetHeight() );
}