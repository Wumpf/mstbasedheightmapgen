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
	///		* 0: ignored
	///		* 1: reset worldSize to a new value. width must be 2 and height 1.
	///			data[0] must be the new worldSizeX and data[1] the one in Y
	///			direction.
	///		* 2: reset the resolution in pixels per world unit. width and
	///			height must be 1. data[0] newly defines PixelPerWorldUnit.
	///		* 3: Set the bottom surface level. All valleys below that value are
	///			set to the given value. This threshold is required because
	///			otherwise the map hight would go to negative infinity.\n
	///			The value must be less than 0 which is the highest value.\n
	///			The initial value is -100.
	///		* 4: Percentage p how much of the mount side is covered by a
	///			quadratic spline. The part [threshold, (1-p)*threshold] is
	///			smoothed with a qudratic spline. Above a linear increase is
	///			used.\n
	///			The initial value is 0.3.
	void SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height);




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
	float _heightThreshold;				/// <<\brief Cuts the distance function at a certain level.
	float _quadraticIncreasePercentage;	/// <<\brief Percentage of the quadratic spline to smooth the mountain foots.
};

