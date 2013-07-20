#pragma once

#include "CommandInfo.h"

// Predeclarations
namespace Json {
	class Value;
};

class GeneratorPipeline
{
private:
	float _worldSizeX;
	float _worldSizeY;
	float _heightMapPixelPerWorldUnit;

	int _numCommands;
	Command** _commands;

	std::unordered_map<std::string, CommandType> _typeMap;
	void InitializeTypeMap();

	Command* LoadBlendCommand( const Json::Value& commandInfo );
	Command* LoadValueNoiseCommand( const Json::Value& commandInfo );
public:
	/// \brief Loads commands from a script.
	/// \param [in] jsonCode An array of commands in form of a json file.
	/// TODO: format description
	CPP_DLL GeneratorPipeline(const std::string& jsonCode);

	/// \brief After load the commands can be executed and the results are
	///		written to the given buffer.
	/// \param [in] resolutionX Target width of the generated map. The map
	///		area is defined by the json input. So the shown section is always
	///		the same. There are just more samples per world unit.
	/// \param [in] resolutionY Target height of the generated map. The map
	///		area is defined by the json input. So the shown section is always
	///		the same. There are just more samples per world unit.
	/// \param [out] finalDestination A buffer where the final result has to
	///		be written into. The buffer must have a size of
	///		resolutionX * resolutionY * sizeof(float)
	/// \param [in] normalizeData true means that all values in
	///		finalDestination will be scaled and offseted to a range from [0,1].
	///		Otherwise the values of finalDestination are in an arbitrary range.
	CPP_DLL void Execute(int resolutionX, int resolutionY, float* finalDestination, bool normalizeData = true);

	CPP_DLL ~GeneratorPipeline();
};