#include "stdafx.h"
#include "CommandBuffer.hpp"
#include "json-parser\json.h"

using namespace std;

void GeneratorPipeline::InitializeTypeMap()
{
	_typeMap.insert(pair<string, CommandType>(string("MST Distance"), CommandType::MST_DISTANCE));
	_typeMap.insert(pair<string, CommandType>(string("MST Inverse Distance"), CommandType::MST_INV_DISTANCE));
	_typeMap.insert(pair<string, CommandType>(string("Value Noise"), CommandType::VALUE_NOISE));
	_typeMap.insert(pair<string, CommandType>(string("ADDITIVE"), CommandType::ADD));
	_typeMap.insert(pair<string, CommandType>(string("MULTIPLICATIVE"), CommandType::MULTIPLY));
	_typeMap.insert(pair<string, CommandType>(string("REFRACTIVE"), CommandType::REFRACT));
	_typeMap.insert(pair<string, CommandType>(string("Smooth"), CommandType::SMOOTH));
	_typeMap.insert(pair<string, CommandType>(string("Normalize"), CommandType::NORMALIZE));
	_typeMap.insert(pair<string, CommandType>(string("NONE"), CommandType::NONE));
}


// ************************************************************************* //
Command* GeneratorPipeline::LoadValueNoiseCommand( const Json::Value& commandInfo )
{
	// Read four additional scalar values
	float heightScale = commandInfo.get("Height", 1.0f).asFloat();
	float gradientDependency = commandInfo.get("GradientDependency", 0.0f).asFloat();
	float heightDependency = commandInfo.get("HeightDependency", 0.0f).asFloat();
	float heightDependencyOffset = commandInfo.get("HeightDependencyOffset", 0.0f).asFloat();
	return new CmdValueNoise(heightScale, gradientDependency, heightDependency, heightDependencyOffset);
}

Command* GeneratorPipeline::LoadBlendCommand( const Json::Value& commandInfo )
{
	// The blending is a subtype of some other types. It is valid if there is
	// no blend operation. Then an overwrite is performed.
	CommandType type = _typeMap[commandInfo.get("Blending", "NONE").asString()];
	switch(type)
	{
		case CommandType::ADD:			return new CmdBlendAdd();
		case CommandType::MULTIPLY:		return new CmdBlendMultiply();
		case CommandType::REFRACT:
			break;
	}
	return nullptr;
}


GeneratorPipeline::GeneratorPipeline(const std::string& jsonCode)
{
	InitializeTypeMap();

	Json::Value root;   // will contains the root value after parsing.
	Json::Reader reader;
	bool parsingSuccessful = reader.parse( jsonCode, root );
	assert(parsingSuccessful);

	// Read general map infos
	_worldSizeX = root["HeightmapWidth"].asFloat();
	_worldSizeY = root["HeightmapHeight"].asFloat();
	_heightMapPixelPerWorldUnit = root["HeightmapPixelPerWorldUnit"].asFloat();

	// Read command array
	Json::Value layers = root["Layers"];
	_numCommands = layers.size();
	// Allocate enough, that each layer can have a blend command
	_commands = new Command*[_numCommands*2];

	for(int i=0; i<_numCommands; ++i)
	{
		CommandType type = _typeMap[layers[i].get("Type", "NONE").asString()];
		switch(type)
		{
		case CommandType::MST_DISTANCE:
			break;
		case CommandType::MST_INV_DISTANCE:
			break;
		case CommandType::VALUE_NOISE:
			_commands[i] = LoadValueNoiseCommand(layers[i]);
			_commands[++i] = LoadBlendCommand(layers[i]);
			++_numCommands;
			break;
		case CommandType::SMOOTH:
			break;
		case CommandType::NORMALIZE:
			break;

		default:
			// Skip unknown layers
			--i;
			--_numCommands;
		}
	}
}

GeneratorPipeline::~GeneratorPipeline()
{
	for(int i=0; i<_numCommands; ++i)
		delete _commands[i];
	delete[] _commands;
}