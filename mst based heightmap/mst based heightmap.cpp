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

	void HeightmapFactory::SetParameter(unsigned int type, array<float, 2>^ data)
	{
		if(_nativeHeightmapFactory)
		{
			pin_ptr<float> pinnedArray = &data[0,0];
			_nativeHeightmapFactory->SetParameter( type, pinnedArray, data->GetLength(1), data->GetLength(0) );
		}
	}
	void HeightmapFactory::GetParameter(unsigned int type, array<float, 2>^ outData, [Out] unsigned int% outWidth, [Out] unsigned int% outHeight )
	{
		if(_nativeHeightmapFactory)
		{
			pin_ptr<float> pinnedArray = &outData[0,0];
			unsigned int tempWidth;
			unsigned int tempHeight;
			_nativeHeightmapFactory->GetParameter( type, pinnedArray, tempWidth, tempHeight);
			outWidth = tempWidth;
			outHeight = tempHeight;
		}
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
		if(_nativeHeightmapFactory)
		{
			pin_ptr<float> pinnedArray = &dataDestination[0,0];
			_nativeHeightmapFactory->Generate(pinnedArray);
		}
	}
}


#pragma unmanaged