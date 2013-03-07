#include "stdafx.h"
#include "HeightmapFactory.h"


HeightmapFactory::HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit) :
	_resolutionX(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_resolutionY(static_cast<unsigned int>(worldSizeX * heightmapPixelPerWorldUnit)),
	_worldSizeX(worldSizeX),
	_worldSizeY(worldSizeY)
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
	for(unsigned int y=0; y<GetHeight(); ++y)
		for(unsigned int x=0; x<GetWidth(); ++x)
			dataDestination[x + y * GetWidth()] = static_cast<float>(rand()) / RAND_MAX;
}