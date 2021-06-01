#pragma once

#include <functional>

#include "DetourCommon.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"

#ifdef _WIN32
#	define DLLEXPORT extern "C" __declspec(dllexport)
#else
#	define DLLEXPORT extern "C"
#endif

enum dtPolyFlags : unsigned short
{
	WALK = 0x01,    // Ability to walk (ground, grass, road)
	SWIM = 0x02,    // Ability to swim (water).
	DOOR = 0x04,    // Ability to move through doors.
	JUMP = 0x08,    // Ability to jump.
	DISABLED = 0x10,    // Disabled polygon
	DOOR_ALB = 0x20,
	DOOR_MID = 0x40,
	DOOR_HIB = 0x80,
	ALL = 0xffff      // All abilities.
};

/*
	[DllImport("ReUth", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	private static extern bool LoadNavMesh(string file, ref IntPtr meshPtr, ref IntPtr queryPtr);

	[DllImport("ReUth", CallingConvention = CallingConvention.Cdecl)]
	private static extern bool FreeNavMesh(IntPtr meshPtr, IntPtr queryPtr);

	[DllImport("ReUth", CallingConvention = CallingConvention.Cdecl)]
	private static extern dtStatus PathStraight(IntPtr queryPtr, float[] start, float[] end, float[] polyPickExt, dtPolyFlags[] queryFilter, dtStraightPathOptions pathOptions, ref int pointCount, float[] pointBuffer, dtPolyFlags[] pointFlags);

	[DllImport("ReUth", CallingConvention = CallingConvention.Cdecl)]
	private static extern dtStatus FindRandomPointAroundCircle(IntPtr queryPtr, float[] center, float radius, float[] polyPickExt, dtPolyFlags[] queryFilter, float[] outputVector);

	[DllImport("ReUth", CallingConvention = CallingConvention.Cdecl)]
	private static extern dtStatus FindClosestPoint(IntPtr queryPtr, float[] center, float[] polyPickExt, dtPolyFlags[] queryFilter, float[] outputVector);
*/

// RAII helper
struct RAII
{
	std::function<void()> cleaner;
	RAII(std::function<void()> cleaner) : cleaner(cleaner) {}
	~RAII() { this->cleaner(); }
};

// missing from Detour?
struct dtNavMeshSetHeader
{
	std::int32_t magic;
	std::int32_t version;
	std::int32_t numTiles;
	dtNavMeshParams params;
};
struct dtNavMeshTileHeader
{
	dtTileRef ref;
	std::int32_t size;
};

DLLEXPORT bool LoadNavMesh(char const* file, dtNavMesh** const mesh, dtNavMeshQuery** const query);
DLLEXPORT bool FreeNavMesh(dtNavMesh* meshPtr, dtNavMeshQuery* queryPtr);
DLLEXPORT dtStatus PathStraight(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], dtStraightPathOptions pathOptions, int* pointCount, float* pointBuffer, dtPolyFlags* pointFlags);
DLLEXPORT dtStatus FindRandomPointAroundCircle(dtNavMeshQuery* query, float center[], float radius, float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector);
DLLEXPORT dtStatus FindClosestPoint(dtNavMeshQuery* query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector);

