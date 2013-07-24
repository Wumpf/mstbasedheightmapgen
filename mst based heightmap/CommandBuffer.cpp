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
	_typeMap.insert(pair<string, CommandType>(string("INTERPOLATE"), CommandType::INTERPOLATE));
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

Command* GeneratorPipeline::LoadMSTDistanceCommand( const Json::Value& commandInfo, bool inverted )
{
	float height = commandInfo.get("Height", 1.0f).asFloat();
	float quadraticSplineHeight = commandInfo.get("QuadraticSpline", 0.3f).asFloat();

	// read point array
	auto pointSetArray = commandInfo.get("PointSet", Json::Value(Json::ValueType::arrayValue));
	int numPoints = pointSetArray.size();
	std::unique_ptr<Vec3[]> points(new Vec3[numPoints]);
//	float maxHeight = 0.0f;
	for(int i=0; i<numPoints; ++i)
	{
		if(pointSetArray[i].size() != 3)
			points[i] = Vec3(0,0,0);
		else
			points[i] = Vec3(pointSetArray[i][0].asFloat(),pointSetArray[i][1].asFloat(),pointSetArray[i][2].asFloat());
//		maxHeight = std::max(maxHeight, points[i].z);
	}

	// scale points to height!
	float scale = height; // height / maxHeight; 
	for(int i=0; i<numPoints; ++i)
		points[i].z *= scale;

	if(inverted)
		return new CmdInvMSTDistance(points.get(), numPoints, height, quadraticSplineHeight);
	else
		return new CmdMSTDistance(points.get(), numPoints, height, quadraticSplineHeight);
}

Command* GeneratorPipeline::LoadBlendCommand( const Json::Value& commandInfo )
{
	// The blending is a subtype of some other types. It is valid if there is
	// no blend operation. Then an overwrite is performed.
	CommandType type = _typeMap[commandInfo.get("Blending", "NONE").asString()];
	float blendFactor = commandInfo.get("BlendFactor", 1.0f).asFloat();
	switch(type)
	{
		case CommandType::ADD:			return new CmdBlendAdd();
		case CommandType::MULTIPLY:		return new CmdBlendMultiply();
		case CommandType::INTERPOLATE :	if(blendFactor != 1.0f) return new CmdBlendInterpolate(blendFactor);	// 1.0 would just copy the last result -> do nothing (overwrite effekt)
										else return nullptr;
		case CommandType::REFRACT:		return new CmdBlendRefract(blendFactor);
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
	_numCommands = 0;
	// Allocate enough, that each layer can have a blend command
	_commands = new Command*[layers.size()*2];
	memset(_commands, 0, sizeof(Command*) * layers.size()*2);

	for(unsigned int jsonLayerIndex=0; jsonLayerIndex<layers.size(); ++jsonLayerIndex)
	{
		CommandType type = _typeMap[layers[jsonLayerIndex].get("Type", "NONE").asString()];
		Json::Value& currentLayer = layers[jsonLayerIndex];
		bool bIsKnown = true;
		switch(type)
		{
		case CommandType::MST_DISTANCE:
			_commands[_numCommands] = LoadMSTDistanceCommand(currentLayer, false);
			break;
		case CommandType::MST_INV_DISTANCE:
			_commands[_numCommands] = LoadMSTDistanceCommand(currentLayer, true);
			break;
		case CommandType::VALUE_NOISE:
			_commands[_numCommands] = LoadValueNoiseCommand(currentLayer);
			break;
	/*	case CommandType::SMOOTH:
			break;
		case CommandType::NORMALIZE:
			break;
			*/
		default:
			// Skip unknown layers - do not add a command
			bIsKnown = false;
		}

		if( bIsKnown )
		{
			// Count the new commando and read its blending
			++_numCommands;
			_commands[_numCommands] = LoadBlendCommand(currentLayer);
			if( _commands[_numCommands] ) _numCommands++;
		}
	}
}

GeneratorPipeline::~GeneratorPipeline()
{
	for(int i=0; i<_numCommands; ++i)
		delete _commands[i];
	delete[] _commands;
}