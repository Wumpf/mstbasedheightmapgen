// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once
#pragma unmanaged

#include <memory>
#include <stdint.h>
#include <assert.h>

#ifdef DLL
#define CPP_DLL __declspec(dllexport)
#else
#define CPP_DLL __declspec(dllimport)
#endif