#include "stdafx.h"
#include "mst based heightmap.h"
#include "HeightmapFactory.h"

#pragma managed

namespace MstBasedHeightmap
{
	HeightmapFactory::HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit) : 
		_nativeHeightmapFactory(new ::HeightmapFactory(worldSizeX, worldSizeY, heightmapPixelPerWorldUnit))
	{

	}

	HeightmapFactory::~HeightmapFactory()
	{
		delete _nativeHeightmapFactory;
	}

	void HeightmapFactory::SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height)
	{
	}

	unsigned int HeightmapFactory::GetWidth()
	{
		if(_nativeHeightmapFactory)
			return _nativeHeightmapFactory->GetWidth();
		else
			return 0;
	}
	
	unsigned int HeightmapFactory::GetHeight()
	{
		if(_nativeHeightmapFactory)
			return _nativeHeightmapFactory->GetHeight();
		else
			return 0;
	}

	void HeightmapFactory::Generate(array<float, 2>^ dataDestination)
	{
		pin_ptr<float> pinnedArray = &dataDestination[0,0];
		_nativeHeightmapFactory->Generate(pinnedArray);
	}
}

#pragma unmanaged