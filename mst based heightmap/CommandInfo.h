#pragma once

#include <functional>

// Predeclartations
namespace OrE {
	namespace ADT {
		class Mesh;
	};
};
struct Vec3;

enum struct CommandType
{
	MST_DISTANCE = 0,
	MST_INV_DISTANCE = 1,
	VALUE_NOISE = 2,
	VORONOI = 3,
	WORLEY_NOISE = 4,
	VORONOISE = 5,

	ADD = 10,
	MULTIPLY = 11,
	REFRACT = 12,
	INTERPOLATE = 13,

	SMOOTH = 100,
	NORMALIZE = 101,

	NONE = 9999
};

/// \brief Description of the map object which is the target of all operations.
/// \details A map is a partition of the whole float x float space. The
///		resolution defines only the sampling rate. Increasing the
///		resolution will not change the large scale appearance (still the
///		same area shown).
///
///		The pixel size can have an aspect != 1 and can be a floating point too.
///		The meaningfulness of the sampling is up to the user.
struct MapBufferInfo
{
	unsigned int ResolutionX;	///< Number of entries in the (rowvise) 2D-buffer in row direction.
	unsigned int ResolutionY;	///< Number of entries in the 2D-buffer in column direction.

	float WorldSizeX;	///< Size of the map section in X direction. This is not the resolution!
	float WorldSizeY;	///< Size of the map section in Y direction. This is not the resolution!

	float HeightmapPixelPerWorldUnit;	///< Determines the resolution / sampling rate of the map section.
	float PixelSize;	///< WorldSize../HeightmapPixelPerWorldUnit
};

/// Base class for any generator command. The derivatives store all information
/// loaded from the json file and some more derived datums which should not be
/// computed per pixel.
/// A layer can be a primitive generator+blending or a filter which is added on
/// top of the current results.
class Command
{
public:
	const CommandType Type;

	Command( CommandType type ) : Type(type) {}

	/// Calculate things like the MST which are not computed per pixel. And do
	/// the other stuff by calling GenerateLayer in parallel.
	/// \param [in] bufferInfo Size parameters for all input and output buffers.
	/// \param [in] prevResult Read access to the result from the second last command.
	///		Depending on the command this must be defined or can be nullptr.
	/// \param [in] currentResult Read access to the result from the last command.
	///		Depending on the command this must be defined or can be nullptr.
	/// \param [out] output Buffer to write the new results into.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) = 0;

	virtual ~Command() {}
};


/// This commando creates a value noise which may depend on the previous step.
///
class CmdValueNoise : public Command
{
	float _heightScale;				///< Amplitude of the noise (all heights in [0,_heightScale]).
	float _gradientDependency;		///< Frequence dependency to the previous slope + slope of smaller frequencies.
	float _heightDependency;		///< Frequence dependency to the previous height + height of smaller frequencies.
	float _heightDependencyOffset;	///< A threshold [0,_heightScale] to control the height dependency.

	float CalculateFrequenceAmplitude( const MapBufferInfo& bufferDesc, float _fCurrentHeight, float _fFrequence, float _fGradientX, float _fGradientY );
	float NoiseKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	// Precomputed values
	int _maxOctave;
public:
	CmdValueNoise( float heightScale,
				   float gradientDependency,
				   float heightDependency,
				   float heightDependencyOffset ) :
		Command(CommandType::VALUE_NOISE),
		_heightScale(heightScale),
		_gradientDependency(gradientDependency/heightScale),
		_heightDependency(heightDependency/heightScale),
		_heightDependencyOffset(heightDependencyOffset*heightScale)
	{}

	/// Create some noise.
	/// \details Uses `currentResult` if defined.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;
};

/// This commando adds the two prior results.
///
class CmdBlendAdd : public Command
{
	float BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );
	float BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

public:
	CmdBlendAdd() : Command(CommandType::ADD) {}

	/// Add two prior results.
	/// \details If `prevResult` is not defined the result is copied from
	///		currentResult.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;
};

/// This commando multiplies the two prior results.
///
class CmdBlendMultiply : public Command
{
	float BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );
	float BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

public:
	CmdBlendMultiply() : Command(CommandType::MULTIPLY) {}

	/// Multiply two prior results.
	/// \details If `prevResult` is not defined the result is copied from
	///		currentResult.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;
};

/// This commando multiplies overwrites the old result, rendering all previous results useless
///
class CmdBlendInterpolate : public Command
{
	float _blendFactor;
public:
	CmdBlendInterpolate(float blendFactor) : Command(CommandType::INTERPOLATE), _blendFactor(blendFactor) {}

	/// Interpolates linear between the old result.
	/// \details `prevResult` will be ignored
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;
};

/// The current result is interpreted as perfect refractive surface and the
/// previous result is distroted.
class CmdBlendRefract : public Command
{
	float BlendKernelNeutral( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );
	float BlendKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	float _refractionDistance;	///< Defines a distance between the two surfaces.
public:
	CmdBlendRefract(float refractionDistance) :
		Command(CommandType::REFRACT),
		_refractionDistance(refractionDistance)
	{}

	/// Distort previous result with the current one.
	/// \details If `prevResult` is not defined the result is copied from
	///		currentResult.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;
};


/// This commando creates the inverse distance field (max-d) of a minimal
/// spanning tree.
class CmdInvMSTDistance : public Command
{
	float GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	OrE::ADT::Mesh* _mst;
	float _height;					///< Maximum height/distance of the ridges and summits.
	float _quadraticSplineHeight;	///< Below this height a spline is used to make fade more smooth
public:
	CmdInvMSTDistance(const Vec3* pointList, int numPoints, float height, float quadraticSplineHeight);

	/// Compute a distance map as new layer.
	/// \details The former results are ignored.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;

	virtual ~CmdInvMSTDistance();
};

/// This commando creates the distance field of a minimal spanning tree.
///
class CmdMSTDistance : public Command
{
	float GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	OrE::ADT::Mesh* _mst;
	float _height;					///< Maximum height/distance of the ridges and summits.
	float _quadraticSplineHeight;	///< Below this height a spline is used to make fade more smooth
public:
	CmdMSTDistance(const Vec3* pointList, int numPoints, float height, float quadraticSplineHeight);

	/// Compute a distance map as new layer.
	/// \details The former results are ignored.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;

	virtual ~CmdMSTDistance();
};


/// The Worley Noise generator creates a cellular shaped noise.
///
class CmdWorly : public Command
{
	float GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	Vec3* _points;			///< All points which show cells (copy). The height (z-coordinate) defines a distance offset.
	float _height;			///< Maximum height/distance scaling factor.
	int _numPoints;			///< Size of the given point set
	int _nthNeighbor;		///< Only the distance to the nth-neighbor is used. The counting starts at 0 (closest point).
public:
	CmdWorly(const Vec3* pointList, int numPoints, int nthNeighbor, float height);

	/// Compute a distance map as new layer.
	/// \details The former results are ignored.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;

	virtual ~CmdWorly();
};


/// Creates a voronoi diagram with some offsetted points.
///
class CmdVoronoi : public Command
{
	float GeneratorKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );

	Vec3* _points;			///< All points which show cells (copy). The height (z-coordinate) defines a distance offset.
	float _height;			///< Maximum height/distance scaling factor.
	int _numPoints;			///< Size of the given point set
public:
	CmdVoronoi(const Vec3* pointList, int numPoints, float height);

	/// Compute a distance map as new layer.
	/// \details The former results are ignored.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination) override;

	virtual ~CmdVoronoi();
};

/// This commando creates a voronoise after Ingo Quilez.
/// http://www.iquilezles.org/www/articles/voronoise/voronoise.htm
class CmdVoronoise : public Command
{
	float _heightScale;				///< Amplitude of the noise (all heights in [0,_heightScale]).
	int _minOctave;					///< Determines largest frequency
	int _maxOctave;					///< Determines smallest frequency _maxOctave >= _minOctave
	float _noiseScaleX;				///< Precomputed scale for the coordinates to frequency
	float _noiseScaleY;				///< Precomputed scale for the coordinates to frequency

	float NoiseKernel( const MapBufferInfo& bufferInfo, int x, int y, const float* prevResult, const float* currentResult );
public:
	CmdVoronoise( float heightScale,
				  int minOctave,
				  int maxOctave ) :
		Command(CommandType::VALUE_NOISE),
		_heightScale(heightScale),
		_minOctave(minOctave),
		_maxOctave(maxOctave)
	{}

	/// Create some noise.
	/// \details Uses `currentResult` if defined.
	virtual void Execute( const MapBufferInfo& bufferInfo,
						  const float* prevResult,
						  const float* currentResult,
						  float* destination ) override;
};


typedef std::function<float(const MapBufferInfo&,int,int,const float*,const float*)> Kernel_t;

/// \brief The "closure" for the parallel executation.
/// \details This struct is filled by Command.Execute and is used as input
///		for GenerateLayer.
struct CommandDesc
{
	const MapBufferInfo& BufferInfo;
	const float* PrevResult;
	const float* CurrentResult;
	Kernel_t Kernel;
	float* Destination;

	CommandDesc(const MapBufferInfo& bufferInfo, const float* prev, const float* current, 
				Kernel_t kernel, float* destination) :
		BufferInfo(bufferInfo),
		PrevResult(prev),
		CurrentResult(current),
		Kernel(kernel),
		Destination(destination)
	{}
};

/// \brief Parallel computation of one layer.
/// \details This method creates as many hardware threads as possible and
///		calculates the new height per pixel.
void GenerateLayer(const CommandDesc& commandInfo);

/// \brief Seqential computation of one layer for testing purposes.
/// \details This method calculates the new height per pixel.
void GenerateLayerSeq(const CommandDesc& commandInfo);

/// \brief Apply a list of loaded commandos to a map.
/// \param [in] commands An array of different command objects
/// \param [in] numCommands Number of object pointers in the array.
/// \param [in] bufferInfo A structured info about buffer size and world coordinates
/// \param [out] finalDestination An buffer where the result of the last
///		operation is saved. The buffer must have the size
///		bufferInfo.ResolutionX * bufferInfo.ResolutionY * sizeof(float)
void ExecuteCommands( Command** commands,
					  int numCommands,
					  const MapBufferInfo& bufferInfo,
					  float* finalDestination );
