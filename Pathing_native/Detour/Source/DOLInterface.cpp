#include <cstdio>
#include <exception>
#include <functional>
#include <iostream>
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

DLLEXPORT bool LoadNavMesh(char const* file, dtNavMesh** const mesh, dtNavMeshQuery** const query)
{
	// load the file
	auto fp = std::fopen(file, "rb");
	if (!fp)
		return false;

	// scope for fp closing
	{
		auto _fpRAII = RAII([=] { std::fclose(fp); });

		dtNavMeshSetHeader header;
		fread(&header, sizeof(header), 1, fp);

		if (header.magic != 0x4d534554 || header.version != 1)
			return false;

		// init mesh and query
		*mesh = dtAllocNavMesh();
		auto status = (*mesh)->init(&header.params);
		if (dtStatusFailed(status))
		{
			dtFreeNavMesh(*mesh);
			*mesh = nullptr;
			return false;
		}
		if (header.numTiles > 0)
		{
			auto tileIdx = 0;
			while (tileIdx < header.numTiles)
			{
				dtNavMeshTileHeader tileHeader;
				fread(&tileHeader, sizeof(tileHeader), 1, fp);
				void* data;
				if (tileHeader.ref == 0 || tileHeader.size == 0 || (data = dtAlloc(tileHeader.size, DT_ALLOC_PERM)) == 0)
					break;
				std::memset(data, 0, tileHeader.size);
				fread(data, tileHeader.size, 1, fp);
				(*mesh)->addTile((unsigned char*)data, tileHeader.size, 1, tileHeader.ref, nullptr);
				tileIdx += 1;
			}
		}

		*query = dtAllocNavMeshQuery();
		status = (*query)->init(*mesh, 2048);
		if (dtStatusFailed(status))
		{
			dtFreeNavMeshQuery(*query);
			*query = nullptr;
			dtFreeNavMesh(*mesh);
			*mesh = nullptr;
			return false;
		}
	}
	return true;
}

DLLEXPORT bool FreeNavMesh(dtNavMesh* meshPtr, dtNavMeshQuery* queryPtr)
{
	if (queryPtr)
		dtFreeNavMeshQuery(queryPtr);
	if (meshPtr)
		dtFreeNavMesh(meshPtr);
	return true;
}

DLLEXPORT dtStatus PathStraight(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], dtStraightPathOptions pathOptions, int* pointCount, float* pointBuffer, dtPolyFlags* pointFlags)
{
	dtStatus status;
	*pointCount = 0;

	dtPolyRef startRef;
	dtPolyRef endRef;
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	if (dtStatusSucceed(status = query->findNearestPoly(start, polyPickExt, &filter, &startRef, nullptr))
		&& dtStatusSucceed(status = query->findNearestPoly(end, polyPickExt, &filter, &endRef, nullptr)))
	{
		int npolys = 0;
		dtPolyRef polys[256];
		if (dtStatusSucceed(status = query->findPath(startRef, endRef, start, end, &filter, polys, &npolys, 256)))
		{
			float epos[3];
			epos[0] = end[0];
			epos[1] = end[1];
			epos[2] = end[2];
			if ((polys[npolys + -1] == endRef) || dtStatusSucceed(status = query->closestPointOnPoly(polys[npolys + -1], end, epos, nullptr)))
			{
				dtPolyRef straightPathPolys[256];
				unsigned char straightPathFlags[256];
				auto straightPathRefs = &straightPathPolys[0];
				if (dtStatusSucceed(status = query->findStraightPath(start, epos, polys, npolys, pointBuffer, straightPathFlags, straightPathRefs, pointCount, 256, pathOptions)) && (0 < *pointCount))
				{
					int pointIdx = 0;
					while (*pointCount != pointIdx && pointIdx <= *pointCount)
					{
						auto ref = *straightPathRefs;
						pointIdx = pointIdx + 1;
						straightPathRefs = straightPathRefs + 1;
						query->getAttachedNavMesh()->getPolyFlags(ref, (unsigned short*)pointFlags);
						pointFlags = pointFlags + 1;
					}
				}
			}
		}
	}
	return status;
}

float frand()
{
	return static_cast<float>(std::rand()) / static_cast<float>(RAND_MAX);
}

DLLEXPORT dtStatus FindRandomPointAroundCircle(dtNavMeshQuery* query, float center[], float radius, float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	dtPolyRef centerRef;
	auto status = query->findNearestPoly(center, polyPickExt, &filter, &centerRef, nullptr);
	if (dtStatusSucceed(status))
	{
		dtPolyRef outRef;
		status = query->findRandomPointAroundCircle(centerRef, center, radius, &filter, frand, &outRef, outputVector);
	}
	return status;
}

DLLEXPORT dtStatus FindClosestPoint(dtNavMeshQuery* query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	dtPolyRef centerRef;
	auto status = query->findNearestPoly(center, polyPickExt, &filter, &centerRef, nullptr);
	if (dtStatusSucceed(status))
		status = query->closestPointOnPoly(centerRef, center, outputVector, nullptr);
	return status;
}
