#pragma once

#include <string>

class CommandBuffer
{
public:
	/// \brief Loads commands from a script.
	/// \param [in] jsonCode An array of commands in form of a json file.
	/// TODO: format description
	CommandBuffer(const std::string& jsonCode);
};