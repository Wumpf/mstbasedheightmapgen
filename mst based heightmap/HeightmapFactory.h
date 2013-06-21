#pragma once

class CPP_DLL HeightmapFactory
{
public:
	/// \brief Creates a new map with the mandatory parameters.
	/// \details A map is a partition of the whole float x float space. The
	///		resolution defines only the sampling rate. Increasing the
	///		resolution will not change the large scale appeareance (still the
	///		same area shown).
	///
	///		The pixels of the map are always squares. If worldSize.. times
	///		heightmapPixelPerWorldUnit is not an integral number this will be
	///		truncated.\n
	///		E.g.: let worldSizeX be 1.15 and heightmapPixelPerWorldUnit 10
	///		than the map would have 11.5 pixels width. This is truncated and
	///		GetWidth would return 11. So the real covered area is 1.1 instead.
	/// \param [in] worldSizeX Size of the map section in X direction. This is
	///		not the resolution!
	/// \param [in] worldSizeY Size of the map section in Y direction. This is
	///		not the resolution!
	/// \param [in] heightmapPixelPerWorldUnit Determines the resolution / 
	///		sampling rate of the map section.
	HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit);
	~HeightmapFactory();



	/// \brief Change a state of the generator.
	/// \details The parameters can be anything. The meaning depends on the
	///		type value.
	/// \param [in] width Data buffer width. Only required if data is an array.
	/// \param [in] height Data buffer height. Only required if data is an array.
	/// \param [in] type
	///		* 0: Select the base map generator. A value of 0 uses the distance
	///			function to an MST. For index 1 the inverse distance map is
	///			used. The inverse function is more intuitive in its
	///			parametrization and is the default generator.
	///		* 1: reset worldSize to a new value. width must be 2 and height 1.
	///			data[0] must be the new worldSizeX and data[1] the one in Y
	///			direction.
	///		* 2: reset the resolution in pixels per world unit. width and
	///			height must be 1. data[0] newly defines PixelPerWorldUnit.
	///		* 3: Set the bottom surface level. All valleys below that value are
	///			set to the given value. This threshold is required because
	///			otherwise the map hight would go to infinity.\n
	///			The value must be greater than 0 which is the smallest value.\n
	///			The initial value is 50.
	///
	///			For negative values the absolute is used.
	///		* 4: Percentage p how much of the mount side is covered by a
	///			quadratic spline. The part [threshold, (1-p)*threshold] is
	///			smoothed with a qudratic spline. Above a linear increase is
	///			used.\n
	///			The initial value is 0.3.
	///		* 5: Set the map seed. The default value is 1111.
	///		* 6: The total amount of noise added to the map. This number is
	///			relative to the maps height. A value of 1 creates (statistically)
	///			a noise with a amplitude like the created base map.
	///		* 7: Influence of the height to the noise frequency. A value of 0
	///			disables the dependency. Too large values cause a very instable
	///			behavior.
	///		* 8: Influence of the gradient to the noise frequency. A value of 0
	///			disables the dependency. Too large values cause a very instable
	///			behavior.
	///		* 9: A list of points describing summits. The height determines the
	///			number of summits whereby each point consists of three floats.
	///			The width is always 3 for the three vector components.
	///		* 10: Intensity of a refraction noise which is applied before the
	///			additional noise to disturb the ridges horizontally.
	///		* 11: READ ONLY: real minimum and maximum height occuring in the terrain.
	void SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height);

	void GetParameter(unsigned int type, float* outData, unsigned int& outWidth, unsigned int& outHeight);



	/// \brief Returns the number of data values in X direction.
	unsigned int GetWidth();
	
	/// \brief Returns the number of data values in Y direction.
	unsigned int GetHeight();

	/// \brief Generates a heightmap with the given settings.
	/// \param [out] dataDestination	Pointer to a preallocated buffer
	///		used as output of the generation. The buffer size must be
	///		GetWidth()*GetHeight()*sizeof(float).
	void Generate(float* dataDestination);


private:
	// Sampling parameters
	unsigned int _resolutionX;
	unsigned int _resolutionY;

	float _worldSizeX;
	float _worldSizeY;

	float _pixelSize;

	// Generation parameters
	Vec3* _summitList;
	unsigned int _numSummits;

	bool _useInverseDistance;			///< Switches between the two generator alternatives
	int _seed;							///< Start value to make generation deterministic.
	float _heightThreshold;				///< Cuts the distance function at a certain level.
	float _quadraticIncreasePercentage;	///< Percentage of the quadratic spline to smooth the mountain foots.

	float _refractionNoiseIntensity;	///< Disturbtion of the terrain by a value noise refraction map.
	float _noiseIntensity;				///< Amount of perlin noise added to the map.
	float _frequencyHeightDependence;	///< Frequence shift depending on the terrain height.
	float _frequencyGradientDependence;	///< Frequence shift depending on the terrain gradient.

	// Statistics.
	float _minHeight;					///< Minimum height in the current generated map
	float _maxHeight;					///< Maximum height in the current generated map
};

