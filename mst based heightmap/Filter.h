#pragma once

/// \brief Transforms the range of values in the float image to the range [0,1].
/// \details
/// \warning For performance reasons height has to be a muliple of 4!
void Normalize( float* dataDestination, int width, int height, float& minHeight, float& maxHeight );


/// \brief Uses several blending methods to add noise to the terrain.
/// \details Adding in this context also means multiplication...
/// \warning For performance reasons height has to be a muliple of 4!
void AddNoise( float* dataDestination, int width, int height, int _iSeed,
			   float frequenceDependencyOffset,
			   float frequenceHeightDependency,
			   float frequenceGradientDependency,
			   float noiseIntensity,
			   float horizontalNoiseScale );

/// \brief Uses a standard value noise normal map and refracts the underlying
///		structure
/// \param [in] refractionDistance The higher the distance between refraction
///		texture and map the heigher the translation of pixels.
void RefractWithNoise( float*& dataDestination, int width, int height, int _iSeed,
			   float refractionDistance );