#include "stdafx.h"

// Include order important! marshal_cppstd.h causes lots of errors otherwise.
#pragma managed
#include <msclr/marshal_cppstd.h>

#pragma unmanaged
#include "CommandBuffer.hpp"
#include "mst based heightmap.h"

#pragma managed

namespace MstBasedHeightmap
{
	// ************************************************************************* //
	GeneratorPipeline::GeneratorPipeline(String^ jsonCode)
	{
		std::string stdString = msclr::interop::marshal_as<std::string>(jsonCode);
		_nativeGenerator = new ::GeneratorPipeline(stdString);
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