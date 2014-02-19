#include <thread>
#include "CommandInfo.h"
#include "math.hpp"

// Uses several blending methods to add noise to the terrain.
static void Line_Kernel( const CommandDesc& commandInfo, int y, int numLines )
{
	for( int i=0; i<numLines; ++i )
	{
		int yw = (y+i) * commandInfo.BufferInfo.ResolutionX;
		for( unsigned int x=0; x<commandInfo.BufferInfo.ResolutionX; ++x )
		{
			commandInfo.Destination[yw+x] = commandInfo.Kernel(commandInfo.BufferInfo, x, y+i, commandInfo.PrevResult, commandInfo.CurrentResult);
		}
	}
}


/// \brief Parallel computation of one layer.
/// \details This method creates as many hardware threads as possible and
///		calculates the new height per pixel.
void GenerateLayer(const CommandDesc& commandInfo)
{
	int n = std::thread::hardware_concurrency();
	// Execution in n threads (one of them is the current one)
	int numLinesPerThread = commandInfo.BufferInfo.ResolutionY / n  +  ( ((commandInfo.BufferInfo.ResolutionY % n)==0) ? 0 : 1 );
	std::thread** threads = new std::thread*[n-1]();	// TODO: Evaluate use of OpenMP http://msdn.microsoft.com/en-us/library/68ah4xc7.aspx
	for( int t=0; t<n-1; ++t )
		threads[t] = new std::thread( Line_Kernel, commandInfo, t*numLinesPerThread, numLinesPerThread );
	int numLines = min(numLinesPerThread, commandInfo.BufferInfo.ResolutionY-(n-1)*numLinesPerThread);
	Line_Kernel( commandInfo, (n-1)*numLinesPerThread, numLines );
	for( int t=0; t<(n-1); ++t )
	{
		threads[t]->join();
		delete threads[t];
	}
	
	delete[] threads;
}

// Seqential computation of one layer for testing purposes.
void GenerateLayerSeq(const CommandDesc& commandInfo)
{
	Line_Kernel( commandInfo, 0, commandInfo.BufferInfo.ResolutionY );
}