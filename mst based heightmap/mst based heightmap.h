#pragma managed

using namespace System;
class HeightmapFactory;

namespace MstBasedHeightmap {

	public ref class HeightmapFactory
	{
	public:

		HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit);
		~HeightmapFactory();

		/// \brief Passes a parameter with the data array to the native engine.
		/// \details The array size is read in the manner data[y][x] which means
		///		dimension 0 is y and dimension 1 is x
		void SetParameter(unsigned int type, array<float, 2>^ data);

		/// \brief Returns the number of data values in X direction
		unsigned int GetWidth();
	
		/// \brief Returns the number of data values in Y direction
		unsigned int GetHeight();

		void Generate(array<float, 2>^ dataDestination);

	private:
		::HeightmapFactory* _nativeHeightmapFactory;
	};
}

#pragma unmanaged