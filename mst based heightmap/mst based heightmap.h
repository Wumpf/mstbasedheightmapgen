#pragma managed

using namespace System::Runtime::InteropServices;

using namespace System;
class HeightmapFactory;
class GeneratorPipeline;

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

		void GetParameter(unsigned int type, array<float, 2>^ outData, [Out] unsigned int% outWidth, [Out] unsigned int% outHeight );

		
		/// \brief Returns the number of data values in X direction
		unsigned int GetWidth();
	
		/// \brief Returns the number of data values in Y direction
		unsigned int GetHeight();

		void Generate(array<float, 2>^ dataDestination);

	private:
		::HeightmapFactory* _nativeHeightmapFactory;
	};

	/// \brief Maps the exported functions of the GeneratorPipeline to a clr
	///		interface.
	public ref class GeneratorPipeline
	{
	public:
		GeneratorPipeline(String^ jsonCode);
		~GeneratorPipeline();

		/// \brief Fills a buffer of arbitrary size with the use of the
		///		current commands.
		/// \details The size of the array determines the sampling only. The
		///		shown sector of the map is defined by the json-command file
		///		during construction.
		/// \param [out] outData The array which is filled with the results.
		///		The size defines the sampling of the map.
		void Execute(array<float, 2>^ outData);
	private:
		::GeneratorPipeline* _nativeGenerator;
	};
}

#pragma unmanaged