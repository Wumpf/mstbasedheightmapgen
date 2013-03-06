#pragma managed

using namespace System;
class HeightmapFactory;

namespace MstBasedHeightmap {

	public ref class HeightmapFactory
	{
	public:

		HeightmapFactory(float worldSizeX, float worldSizeY, float heightmapPixelPerWorldUnit);
		~HeightmapFactory();

		void SetParameter(unsigned int type, const float* data, unsigned int width, unsigned int height);

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