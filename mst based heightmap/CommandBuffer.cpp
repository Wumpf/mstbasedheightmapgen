#include <cassert>
#include "CommandBuffer.hpp"
#include "json-parser\json.h"

using namespace std;

void CommandBuffer::InitializeTypeMap()
{
	_typeMap.insert(pair<string, CommandType>(string("MST Distance"), CommandType::MST_DISTANCE));
	_typeMap.insert(pair<string, CommandType>(string("MST Inverse Distance"), CommandType::MST_INV_DISTANCE));
	_typeMap.insert(pair<string, CommandType>(string("Value Noise"), CommandType::VALUE_NOISE));
	_typeMap.insert(pair<string, CommandType>(string("ADDITIVE"), CommandType::ADD));
	_typeMap.insert(pair<string, CommandType>(string("MULTIPLICATIVE"), CommandType::MULTIPLY));
	_typeMap.insert(pair<string, CommandType>(string("REFRACTIVE"), CommandType::REFRACT));
	_typeMap.insert(pair<string, CommandType>(string("Smooth"), CommandType::SMOOTH));
	_typeMap.insert(pair<string, CommandType>(string("Normalize"), CommandType::NORMALIZE));
}



CommandBuffer::CommandBuffer(const std::string& jsonCode)
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
	_commands = new Command*[_numCommands];

	for(int i=0; i<_numCommands; ++i)
	{
		CommandType type = _typeMap[layers[i].get("Type", "NONE").asString()];
	}
}

CommandBuffer::~CommandBuffer()
{
	for(int i=0; i<_numCommands; ++i)
		delete _commands[i];
	delete[] _commands;
}