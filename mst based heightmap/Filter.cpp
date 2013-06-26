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
void Normalize( float* dataDestination, int width, int height, float& minHeight, float& maxHeight )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	// Search min and max sequentially (return values need so much code)
	minHeight = *dataDestination;
	maxHeight = *dataDestination;
	for( int y=0; y<height; ++y )
	{
		int yw = y*width;
		for( int x=0; x<height; ++x )
		{
			float value = dataDestination[x+yw];
			minHeight = min( value, minHeight );
			maxHeight = max( value, maxHeight );
		}
	}

	float rangeInv = 1.0f / (maxHeight-minHeight);

	std::thread* threads[8];
	for( int y=0; y<height; y+=32 )
	{
		int num = min(8,(height-y)/4);
		
		for( int t=0; t<num; ++t )
			threads[t] = new std::thread( Normalize_Kernel, dataDestination, width, (y+t*4)*width, minHeight, rangeInv );
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
	int maxOctave;

	float frequenceDependencyOffset;
	float frequenceHeightDependency;
	float frequenceGradientDependency;
	float noiseIntensity;
	float horizontalNoiseScale;
};



float calculateFrequenceAmplitude( const AddNoiseParam& bufferDesc, float _fCurrentHeight, float _fFrequence, float _fGradientX, float _fGradientY )
{
	float fHeightDependency = exp( (_fCurrentHeight-bufferDesc.frequenceDependencyOffset) * bufferDesc.frequenceHeightDependency);
	float fGradientDependency = 1.0f + sqrt(_fGradientX*_fGradientX + _fGradientY*_fGradientY) * bufferDesc.frequenceGradientDependency;
	return min( 6.0f, fHeightDependency*fGradientDependency ) / _fFrequence;
}

// Uses several blending methods to add noise to the terrain.
static void AddNoise_Kernel( AddNoiseParam* bufferDesc, int y, int numLines )
{
	for( int i=0; i<numLines; ++i )
	{
		int yw = (y+i) * bufferDesc->width;
		float fy = (y+i) * bufferDesc->horizontalNoiseScale;
		for( int x=0; x<bufferDesc->width; ++x )
		{
			float fGX = 0.0f;
			float fGY = 0.0f;
			float fSum = bufferDesc->data[yw+x];
			float fx = bufferDesc->horizontalNoiseScale * x;

			// *************** Noise function ***************
			for( int i=0; i<bufferDesc->maxOctave; ++i )
			{
				float fdX;
				float fdY;
				float fFrequence = float(1<<i);
				float fAmplitude = calculateFrequenceAmplitude( *bufferDesc, fSum, fFrequence, fGX, fGY ) * bufferDesc->noiseIntensity;
				//fSum += abs(Rand2D( fx, fy, fFrequence, fdX, fdY ) - 0.5f) * fAmplitude;
				fSum += (Rand2D( fx, fy, fFrequence, fdX, fdY ) * 2.0f - 1.0f) * fAmplitude;
				// Update global gradient
				fGX += fdX*fFrequence*fAmplitude;		fGY += fdY*fFrequence*fAmplitude;
			}

			bufferDesc->data[yw+x] = fSum;
			//bufferDesc->data[yw+x] = Rand2D( 0, 4, 0.5f, x*0.01f, (y+i)*0.01f, 1, fGX, fGY );
		}
	}
}

void AddNoise( float* dataDestination, int width, int height, int _iSeed,
			   float frequenceDependencyOffset,
			   float frequenceHeightDependency,
			   float frequenceGradientDependency,
			   float noiseIntensity,
			   float horizontalNoiseScale )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	SetSeed( _iSeed );
	AddNoiseParam bufferDesc = { dataDestination, width, height, int(log( max(width, height) )/log(2)),
		frequenceDependencyOffset, frequenceHeightDependency, frequenceGradientDependency, noiseIntensity, horizontalNoiseScale };

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


// ************************************************************************* //
struct RefractionParam {
	float* data;
	float* out;
	int width;
	int height;
	int maxOctave;
	float horizontalNoiseScale;
	float refractionDistance;
};

static void Refraction_Kernel( RefractionParam* bufferDesc, int y, int numLines )
{
	for( int i=0; i<numLines; ++i )
	{
		int yw = (y+i) * bufferDesc->width;
		float fy = (y+i) * bufferDesc->horizontalNoiseScale;
		for( int x=0; x<bufferDesc->width; ++x )
		{
			float fGradX, fGradY;
			Rand2D( 0, bufferDesc->maxOctave, 0.25f, x*bufferDesc->horizontalNoiseScale, fy,
				fGradX, fGradY );
			
			float fNorm = 1.0f/sqrt(fGradX*fGradX + 1 + fGradY*fGradY);
			fGradX *= fNorm;
			fGradY *= fNorm;

			float fStep = 5.0f* bufferDesc->refractionDistance / fNorm;

			float x_refrac = max(0.0f,min(float(bufferDesc->width-1), x + fGradX * fStep));
			float y_refrac = max(0.0f,min(float(bufferDesc->height-1), y + i + fGradY * fStep));
			int dx = Floor(x_refrac);	x_refrac -= dx;
			int dy = Floor(y_refrac);	y_refrac -= dy;
			bufferDesc->out[yw+x] = lrp(lrp(bufferDesc->data[dy * bufferDesc->width + dx], bufferDesc->data[dy * bufferDesc->width + dx + 1], x_refrac),
				lrp(bufferDesc->data[(dy+1) * bufferDesc->width + dx], bufferDesc->data[(dy+1) * bufferDesc->width + dx + 1], x_refrac), y_refrac);
		}
	}
}

void RefractWithNoise( float*& dataDestination, int width, int height, int _iSeed,
			   float refractionDistance )
{
	// Hight must be devisible through 4
	assert( (height & 3) == 0 );

	SetSeed( _iSeed );
	float* dataCopy = new float[width*height];
	memcpy( dataCopy, dataDestination, sizeof(float)*width*height );
	RefractionParam bufferDesc = { dataCopy, dataDestination, width, height,
		int(0.5*log( max(width, height) )/log(2)), 0.01f, refractionDistance };

	//Refraction_Kernel( &bufferDesc, 0, height );

	// Execution in 8 threads (one of them is the current one)
	int numLinesPerThread = height / 8  +  ( ((height&7)==0) ? 0 : 1 );
	std::thread* threads[7];
	for( int t=0; t<7; ++t )
		threads[t] = new std::thread( Refraction_Kernel, &bufferDesc, t*numLinesPerThread, numLinesPerThread );
	int numLines = min(numLinesPerThread, height-7*numLinesPerThread);
	Refraction_Kernel( &bufferDesc, 7*numLinesPerThread, numLines );
	for( int t=0; t<7; ++t )
	{
		threads[t]->join();
		delete threads[t];
	}

	// Remove double buffer
	delete[] dataCopy;
}