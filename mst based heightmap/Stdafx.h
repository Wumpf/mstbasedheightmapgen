// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once
#pragma unmanaged

#include <memory>
#include <stdint.h>
#include <assert.h>
#include <string>
#include <unordered_map>

// Stuff copied from OrBaseLib
#include "src-mst/OrADTObjects.h"
#include "src-mst/OrHeap.h"
#include "src-mst/OrHash.h"
#include "src-mst/OrGraph.h"

#ifdef DLL
#define CPP_DLL __declspec(dllexport)
#else
#define CPP_DLL __declspec(dllimport)
#endif