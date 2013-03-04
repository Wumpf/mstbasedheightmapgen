#pragma once

class HeightmapFactory
{
public:
	HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit);
	~HeightmapFactory();

	void SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height);

	/// \brief Returns the number of data values in X direction
	unsigned int GetWidth();
	
	/// \brief Returns the number of data values in Y direction
	unsigned int GetHeight();

	/// \brief Generates a heightmap with the given settings
	/// \param dataDestination		pointer to preallocated data to write the heightmap in
	void Generate(float* dataDestination);
};

