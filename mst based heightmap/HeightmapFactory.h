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
	unsigned int _resolutionX;
	unsigned int _resolutionY;

	float _worldSizeX;
	float _worldSizeY;

	float _pixelSize;
};

