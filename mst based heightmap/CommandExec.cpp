#include "CommandInfo.h"

void ExecuteCommands(Command** commands, int numCommands, const MapBufferInfo& bufferInfo, float* finalDestination)
{
	// Acquire a triple buffer
	size_t bufferSize = bufferInfo.ResolutionX * bufferInfo.ResolutionY * sizeof(float);
	float* buffer[3] = {(float*)malloc(bufferSize), (float*)malloc(bufferSize), (float*)malloc(bufferSize)};

	float* last = nullptr;
	float* current = nullptr;
	int destIndex = 0;
	for(int i=0; i<numCommands; ++i)
	{
		// Write to the temporary buffer except for the last command. Write to
		// final destination instead.
		commands[i]->Execute(bufferInfo, last, current, (i==numCommands-1 ? finalDestination : buffer[destIndex]));
		// Toggle the 3 buffers. For the last one this is irrelevant.
		last = current;
		current = buffer[destIndex];
		destIndex = (destIndex + 1) % 3;
	}

	free(buffer[0]);
	free(buffer[1]);
	free(buffer[2]);
}