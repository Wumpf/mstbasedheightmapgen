#include "Stdafx.h"
#include "CommandBuffer.hpp"
#include "Filter.h"

void GeneratorPipeline::Execute(int resolutionX, int resolutionY, float* finalDestination, bool normalizeData)
//void ExecuteCommands(Command** commands, int numCommands, const MapBufferInfo& bufferInfo, float* finalDestination)
{
	// Acquire a triple buffer
	size_t bufferSize = resolutionX * resolutionY * sizeof(float);
	float* buffer[3] = {(float*)malloc(bufferSize), (float*)malloc(bufferSize), (float*)malloc(bufferSize)};

	// Put all buffer related things together
	MapBufferInfo bufferInfo;
	bufferInfo.ResolutionX = resolutionX;
	bufferInfo.ResolutionY = resolutionY;
	bufferInfo.WorldSizeX = _worldSizeX;
	bufferInfo.WorldSizeY = _worldSizeY;
	bufferInfo.PixelSize = _worldSizeX / resolutionX;
	bufferInfo.HeightmapPixelPerWorldUnit = 1.0f / bufferInfo.PixelSize;

	float* last = nullptr;
	float* current = nullptr;
	int destIndex = 0;
	for(int i=0; i<_numCommands; ++i)
	{
		// Write to the temporary buffer except for the last command. Write to
		// final destination instead.
		_commands[i]->Execute(bufferInfo, last, current, (i==_numCommands-1 ? finalDestination : buffer[destIndex]));
		// Toggle the 3 buffers. For the last one this is irrelevant.
		last = current;
		current = buffer[destIndex];
		destIndex = (destIndex + 1) % 3;
	}

	free(buffer[0]);
	free(buffer[1]);
	free(buffer[2]);


	// normalize data
	if(normalizeData)
	{
		float minHeight = std::numeric_limits<float>::max();
		float maxHeight = std::numeric_limits<float>::min();
		for(int i=0; i< resolutionX*resolutionY; ++i)
		{
			minHeight = std::min(finalDestination[i], minHeight);
			maxHeight = std::min(finalDestination[i], maxHeight);
		}
		Normalize(finalDestination, resolutionX, resolutionY, minHeight, maxHeight);
	}
}