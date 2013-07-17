#pragma once

#include <string>
#include <unordered_map>
#include "CommandInfo.h"

class CommandBuffer
{
private:
	float _worldSizeX;
	float _worldSizeY;
	float _heightMapPixelPerWorldUnit;

	int _numCommands;
	Command** _commands;

	std::unordered_map<std::string, CommandType> _typeMap;
	void InitializeTypeMap();
public:
	/// \brief Loads commands from a script.
	/// \param [in] jsonCode An array of commands in form of a json file.
	/// TODO: format description
	CommandBuffer(const std::string& jsonCode);

	~CommandBuffer();
};