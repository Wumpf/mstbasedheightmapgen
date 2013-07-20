#include "stdafx.h"

// Include order important! marshal_cppstd.h causes lots of errors otherwise.
#pragma managed
#include <msclr/marshal_cppstd.h>

#pragma unmanaged
#include "HeightmapFactory.h"
#include "CommandBuffer.hpp"
#include "mst based heightmap.h"

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



	// ************************************************************************* //
	GeneratorPipeline::GeneratorPipeline(String^ jsonCode)
	{
		std::string stdString = msclr::interop::marshal_as<std::string>(jsonCode);
		_nativeGenerator = new ::GeneratorPipeline("erg");
	}

	GeneratorPipeline::~GeneratorPipeline()
	{
		delete _nativeGenerator;
	}

	void GeneratorPipeline::Execute(array<float, 2>^ outData)
	{
		pin_ptr<float> pinnedArray = &outData[0,0];
		_nativeGenerator->Execute(outData->GetLength(1), outData->GetLength(0), pinnedArray);
	}
}


#pragma unmanaged