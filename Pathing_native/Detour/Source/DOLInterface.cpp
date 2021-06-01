#include <cstdio>
#include <exception>
#include <functional>
#include <iostream>
#include <random>

#include "DOLInterface.h"

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

void PathOptimize(int* pointCount, float* pointBuffer, dtPolyRef* refs)
{
	for (int i = 0; i < *pointCount - 2; ++i)
	{
		// we take 3 points: first --- mid --- last and check if mid is on the line, in this case, we remove mid
		float d[3] = { // last - first
			pointBuffer[(i + 2) * 3 + 0] - pointBuffer[i * 3 + 0],
			pointBuffer[(i + 2) * 3 + 1] - pointBuffer[i * 3 + 1],
			pointBuffer[(i + 2) * 3 + 2] - pointBuffer[i * 3 + 2],
		};
		float e[3] = { // mid - first
			pointBuffer[(i + 1) * 3 + 0] - pointBuffer[i * 3 + 0],
			pointBuffer[(i + 1) * 3 + 1] - pointBuffer[i * 3 + 1],
			pointBuffer[(i + 1) * 3 + 2] - pointBuffer[i * 3 + 2],
		};

		if (e[0] != 0 || e[1] != 0 || e[2] != 0)
		{
			float dot = dtVdot(d, e);
			float res = (dot * dot) / dtVdot(d, d) / dtVdot(e, e);
			if (res >= 0.9999)
			{
				std::copy(pointBuffer + (i + 2) * 3, pointBuffer + (*pointCount) * 3, pointBuffer + (i + 1) * 3);
				std::copy(refs + i + 2, refs + *pointCount, refs + i + 1);
				*pointCount -= 1;
				--i; // we redo this loop
			}
		}
	}
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
					PathOptimize(pointCount, pointBuffer, straightPathRefs);
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

thread_local std::mt19937 rngMt = std::mt19937(std::random_device{}());
thread_local std::uniform_real_distribution<float> rng(0.0f, 1.0f);

float frand()
{
	return rng(rngMt);
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
