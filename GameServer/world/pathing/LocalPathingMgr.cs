using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// Pathing
	/// </summary>
	public class LocalPathingMgr : IPathingMgr
	{
		public const float CONVERSION_FACTOR = 1.0f / 32f;
		private const float INV_FACTOR = (1f / CONVERSION_FACTOR);

		[Flags]
		private enum dtStatus : uint
		{
			// High level status.
			DT_SUCCESS = 1u << 30,        // Operation succeed.

			// Detail information for status.
			DT_PARTIAL_RESULT = 1 << 6,     // Query did not reach the end location, returning best guess. 
		}

		public enum dtStraightPathOptions : uint
		{
			DT_STRAIGHTPATH_NO_CROSSINGS = 0x00,    // Do not add extra vertices on polygon edge crossings.
			DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01,  // Add a vertex at every polygon edge crossing where area changes.
			DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02,     // Add a vertex at every polygon edge crossing.
		}

		private const int MAX_POLY = 256;    // max vector3 when looking up a path (for straight paths too)

		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static Dictionary<ushort, IntPtr[]> _navmeshPtrs = new Dictionary<ushort, IntPtr[]>();

		[DllImport("dol_detour", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern bool LoadNavMesh(string file, ref IntPtr meshPtr, ref IntPtr queryPtr);

		[DllImport("dol_detour", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool FreeNavMesh(IntPtr meshPtr, IntPtr queryPtr);

		[DllImport("dol_detour", CallingConvention = CallingConvention.Cdecl)]
		private static extern dtStatus PathStraight(IntPtr queryPtr, float[] start, float[] end, float[] polyPickExt, dtPolyFlags[] queryFilter, dtStraightPathOptions pathOptions, ref int pointCount, float[] pointBuffer, dtPolyFlags[] pointFlags);

		[DllImport("dol_detour", CallingConvention = CallingConvention.Cdecl)]
		private static extern dtStatus FindRandomPointAroundCircle(IntPtr queryPtr, float[] center, float radius, float[] polyPickExt, dtPolyFlags[] queryFilter, float[] outputVector);

		[DllImport("dol_detour", CallingConvention = CallingConvention.Cdecl)]
		private static extern dtStatus FindClosestPoint(IntPtr queryPtr, float[] center, float[] polyPickExt, dtPolyFlags[] queryFilter, float[] outputVector);

		/// <summary>
		/// Initializes the PathingMgr  by loading all available navmeshes
		/// </summary>
		/// <returns></returns>
		public bool Init()
		{
			try
			{
				var dummy = IntPtr.Zero;
				LoadNavMesh("this file does not exists!", ref dummy, ref dummy);
			}
			catch (Exception e)
			{
				log.ErrorFormat("The current process is a {0} bit process!", (IntPtr.Size == 8 ? "64bit" : "32bit"));
				log.ErrorFormat("PathingMgr did not find the ReUth.dll! Starting server anyway but pathing will not work! Error message: {0}", e.ToString());
				return false;
			}

			_navmeshPtrs = new Dictionary<ushort, IntPtr[]>();
			foreach (var zone in WorldMgr.Zones.Values)
			{
				LoadNavMesh(zone);
			}
			return true;
		}

		/// <summary>
		/// Loads the navmesh for the specified zone (if available)
		/// </summary>
		/// <param name="zone"></param>
		public void LoadNavMesh(Zone zone)
		{
			if (_navmeshPtrs.ContainsKey(zone.ID))
				throw new Exception($"Loading NavMesh failed for zone {zone.ID}: already loaded");
			var id = zone.ID;
			var file = $"pathing{Path.DirectorySeparatorChar}zone{id:D3}.nav";
			file = Path.GetFullPath(file); // not sure if c dll can load relative stuff
			if (!File.Exists(file))
			{
				//log.ErrorFormat("Loading NavMesh failed for zone {0}! (File not found: {1})", id, file);
				return;
			}

			var meshPtr = IntPtr.Zero;
			var queryPtr = IntPtr.Zero;

			if (!LoadNavMesh(file, ref meshPtr, ref queryPtr))
			{
				log.ErrorFormat("Loading NavMesh failed for zone {0}!", id);
				return;
			}

			if (meshPtr == IntPtr.Zero || queryPtr == IntPtr.Zero)
			{
				log.ErrorFormat("Loading NavMesh failed for zone {0}! (Pointer was zero!)", id);
				return;
			}
			log.InfoFormat("Loading NavMesh sucessful for zone {0}", id);
			_navmeshPtrs[zone.ID] = new[] { meshPtr, queryPtr };
			zone.IsPathingEnabled = true;
		}

		/// <summary>
		/// Unloads the navmesh for a specific zone
		/// </summary>
		/// <param name="zone"></param>
		public void UnloadNavMesh(Zone zone)
		{
			if (_navmeshPtrs.ContainsKey(zone.ID))
			{
				zone.IsPathingEnabled = false;
				var ptrs = _navmeshPtrs[zone.ID];
				FreeNavMesh(ptrs[0], ptrs[1]);
				_navmeshPtrs.Remove(zone.ID);
			}
		}

		/// <summary>
		/// Stops the PathingMgr and releases all loaded navmeshes
		/// </summary>
		public void Stop()
		{
			if (_navmeshPtrs == null) return;
			foreach (var ptrs in _navmeshPtrs.Values)
				FreeNavMesh(ptrs[0], ptrs[1]);
			_navmeshPtrs = null;
		}

		/// <summary>
		/// Returns a path that prevents collisions with the navmesh, but floats freely otherwise
		/// </summary>
		/// <param name="zone"></param>
		/// <param name="start">Start in GlobalXYZ</param>
		/// <param name="end">End in GlobalXYZ</param>
		/// <returns></returns>
		public async Task<WrappedPathingResult> GetPathStraightAsync(Zone zone, Vector3 start, Vector3 end)
		{
			if (!_navmeshPtrs.ContainsKey(zone.ID))
				return new WrappedPathingResult
				{
					Error = PathingError.NoPathFound,
					Points = null,
				};
			//GSStatistics.Paths.Inc();

			return await Task.Factory.StartNew(() =>
			{
				var ptrs = _navmeshPtrs[zone.ID];
				var startFloats = (start + Vector3.UnitZ * 8).ToRecastFloats();
				var endFloats = (end + Vector3.UnitZ * 8).ToRecastFloats();

				var numNodes = 0;
				var buffer = new float[MAX_POLY * 3];
				var flags = new dtPolyFlags[MAX_POLY];
				dtPolyFlags includeFilter = dtPolyFlags.ALL ^ dtPolyFlags.DISABLED;
				dtPolyFlags excludeFilter = 0;
				float polyExtX = 64.0f;
				float polyExtY = 64.0f;
				float polyExtZ = 256.0f;
				dtStraightPathOptions options = dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS;
				var filter = new[] { includeFilter, excludeFilter };
				var status = PathStraight(ptrs[1], startFloats, endFloats, new Vector3(polyExtX, polyExtY, polyExtZ).ToRecastFloats(), filter, options, ref numNodes, buffer, flags);
				if ((status & dtStatus.DT_SUCCESS) == 0)
				{
					return new WrappedPathingResult
					{
						Error = PathingError.NoPathFound,
						Points = null,
					};
				}

				var points = new WrappedPathPoint[numNodes];
				var positions = Vector3ArrayFromRecastFloats(buffer, numNodes);

				for (var i = 0; i < numNodes; i++)
				{
					points[i].Position = positions[i];
					points[i].Flags = flags[i];
				}

				if ((status & dtStatus.DT_PARTIAL_RESULT) == 0)
				{
					return new WrappedPathingResult
					{
						Error = PathingError.PathFound,
						Points = points,
					};
				}
				else
				{
					return new WrappedPathingResult
					{
						Error = PathingError.PartialPathFound,
						Points = points,
					};
				}
			}, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Returns a random point on the navmesh around the given position
		/// </summary>
		/// <param name="zone">Zone</param>
		/// <param name="position">Start in GlobalXYZ</param>
		/// <param name="radius">End in GlobalXYZ</param>
		/// <returns>null if no point found, Vector3 with point otherwise</returns>
		public async Task<Vector3?> GetRandomPointAsync(Zone zone, Vector3 position, float radius)
		{
			if (!_navmeshPtrs.ContainsKey(zone.ID))
				return null;

			//GSStatistics.Paths.Inc();

			return await Task.Factory.StartNew(() =>
			{
				var ptrs = _navmeshPtrs[zone.ID];
				var center = (position + Vector3.UnitZ * 8).ToRecastFloats();
				var cradius = (radius * CONVERSION_FACTOR);
				var outVec = new float[3];

				var defaultInclude = (dtPolyFlags.ALL ^ dtPolyFlags.DISABLED);
				var defaultExclude = (dtPolyFlags)0;
				var filter = new dtPolyFlags[] { defaultInclude, defaultExclude };

				var polyPickEx = new float[3] { 2.0f, 4.0f, 2.0f };

				var status = FindRandomPointAroundCircle(ptrs[1], center, cradius, polyPickEx, filter, outVec);

				if ((status & dtStatus.DT_SUCCESS) == 0)
				{
					return (Vector3?)null;
				}

				return new Vector3(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);
			}, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Returns the closest point on the navmesh (UNTESTED! EXPERIMENTAL! WILL GO SUPERNOVA ON USE! MAYBE!?)
		/// </summary>
		public Task<Vector3?> GetClosestPointAsync(Zone zone, Vector3 position, float xRange = 256f, float yRange = 256f, float zRange = 256f)
		{
			if (!_navmeshPtrs.ContainsKey(zone.ID))
				return Task.FromResult<Vector3?>(position); // Assume the point is safe if we don't have a navmesh
			//GSStatistics.Paths.Inc();

			return Task.Factory.StartNew(() =>
			{
				var ptrs = _navmeshPtrs[zone.ID];
				var center = (position + Vector3.UnitZ * 8).ToRecastFloats();
				var outVec = new float[3];

				var defaultInclude = (dtPolyFlags.ALL ^ dtPolyFlags.DISABLED);
				var defaultExclude = (dtPolyFlags)0;
				var filter = new dtPolyFlags[] { defaultInclude, defaultExclude };

				var polyPickEx = new Vector3(xRange, yRange, zRange).ToRecastFloats();

				var status = FindClosestPoint(ptrs[1], center, polyPickEx, filter, outVec);

				if ((status & dtStatus.DT_SUCCESS) == 0)
				{
					return (Vector3?)null;
				}

				return new Vector3(outVec[0] * INV_FACTOR, outVec[2] * INV_FACTOR, outVec[1] * INV_FACTOR);
			}, TaskCreationOptions.LongRunning);
		}

		private Vector3[] Vector3ArrayFromRecastFloats(float[] buffer, int numNodes)
		{
			var result = new Vector3[numNodes];
			for (var i = 0; i < numNodes; i++)
				result[i] = new Vector3(buffer[i * 3 + 0] * INV_FACTOR, buffer[i * 3 + 2] * INV_FACTOR, buffer[i * 3 + 1] * INV_FACTOR);
			return result;
		}

		/// <summary>
		/// True if pathing is enabled for the specified zone
		/// </summary>
		/// <param name="zone"></param>
		/// <returns></returns>
		public bool HasNavmesh(Zone zone)
		{
			return zone != null && _navmeshPtrs.ContainsKey(zone.ID);
		}

		public bool IsAvailable => true;
	}
}
