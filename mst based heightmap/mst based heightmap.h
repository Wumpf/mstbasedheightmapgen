#pragma managed

using namespace System::Runtime::InteropServices;

using namespace System;
class HeightmapFactory;
class GeneratorPipeline;

namespace MstBasedHeightmap {

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