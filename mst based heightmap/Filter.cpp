#include <thread>
#include <assert.h>
#include "math.hpp"
#include "Filter.h"
#include "Noise.h"

static void Normalize_Kernel( float* dataDestination, int width, int yw, float fmin, float rangeInv )
{
	int yw1 = yw+width;
	int yw2 = yw+width*2;
	int yw3 = yw+width*3;
	for( int x=0; x<width; ++x )
	{
		dataDestination[x+yw ] = ( dataDestination[x+yw ] - fmin ) * rangeInv;
		dataDestination[x+yw1] = ( dataDestination[x+yw1] - fmin ) * rangeInv;
		dataDestination[x+yw2] = ( dataDestination[x+yw2] - fmin ) * rangeInv;
		dataDestination[x+yw3] = ( dataDestination[x+yw3] - fmin ) * rangeInv;
	/*	dataDestination[x+yw ] *= dataDestination[x+yw ];
		dataDestination[x+yw1] *= dataDestination[x+yw1];
		dataDestination[x+yw2] *= dataDestination[x+yw2];
		dataDestination[x+yw3] *= dataDestination[x+yw3];*/
	}
}

// Transforms the range of values in the float image to the range [0,1].
void Normalize( float* dataDestination, int width, int height )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	// Search min and max sequentially (return values need so much code)
	float fmin = *dataDestination;
	float fmax = *dataDestination;
	for( int y=0; y<height; ++y )
	{
		int yw = y*width;
		for( int x=0; x<height; ++x )
		{
			float value = dataDestination[x+yw];
			fmin = min( value, fmin );
			fmax = max( value, fmax );
		}
	}

	float rangeInv = 1.0f / (fmax-fmin);

	std::thread* threads[8];
	for( int y=0; y<height; y+=32 )
	{
		int num = min(8,(height-y)/4);
		
		for( int t=0; t<num; ++t )
			threads[t] = new std::thread( Normalize_Kernel, dataDestination, width, (y+t*4)*width, fmin, rangeInv );
		for( int t=0; t<num; ++t )
		{
			threads[t]->join();
			delete threads[t];
		}
	}
}


// ************************************************************************* //
// Parameter given to the threads of the AddNoiseKernel
struct AddNoiseParam {
	float* data;
	int width;
	int height;
};

// Uses several blending methods to add noise to the terrain.
static void AddNoise_Kernel( AddNoiseParam* bufferDesc, int y, int numLines )
{
	float fGX;
	float fGY;
	for( int i=0; i<numLines; ++i )
	{
		int yw = (y+i)*bufferDesc->width;
		for( int x=0; x<bufferDesc->width; ++x )
		{
			bufferDesc->data[yw+x] *= Rand2D( 0, 4, 0.5f, x*0.01f, (y+i)*0.01f, 1, fGX, fGY );
		}
	}
}

void AddNoise( float* dataDestination, int width, int height )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	AddNoiseParam bufferDesc = {dataDestination, width, height};

	//AddNoise_Kernel( &bufferDesc, 0, height );

	// Execution in 8 threads (one of them is the current one)
	int numLinesPerThread = height / 8  +  ( ((height&7)==0) ? 0 : 1 );
	std::thread* threads[7];
	for( int t=0; t<7; ++t )
		threads[t] = new std::thread( AddNoise_Kernel, &bufferDesc, t*numLinesPerThread, numLinesPerThread );
	int numLines = min(numLinesPerThread, height-7*numLinesPerThread);
	AddNoise_Kernel( &bufferDesc, 7*numLinesPerThread, numLines );
	for( int t=0; t<7; ++t )
	{
		threads[t]->join();
		delete threads[t];
	}
}