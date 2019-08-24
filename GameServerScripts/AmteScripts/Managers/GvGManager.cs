using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using log4net;

namespace AmteScripts.Managers
{
	public interface IGvGArea
	{
		bool Active { get; set; }
		Guild Guild { get; set; }
		IList<IGvGGuard> Gardes { get; set; }
		bool TakeControl(GamePlayer player, bool force);
	}

	public interface IGvGGuard
	{
	}

	public static class GvGManager
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Dictionary<int, IGvGArea> _areas = new Dictionary<int, IGvGArea>();

		#region Initialisation
		[GameServerStartedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBGvGArea));
			_log.Info("GvG manager initialized: " + _LoadAreas());
		}

		private static bool _LoadAreas()
		{
			GameServer.Database.SelectAllObjects<DBGvGArea>().Foreach(
				area =>
				{
					var a = _CreateArea(area);
					if (a != null)
						_areas.Add(area.ID, a);
				});
			return true;
		}

		private static IGvGArea _CreateArea(DBGvGArea db)
		{
			IGvGArea area = null;

			foreach (var script in ScriptMgr.Scripts.Union(new[] { typeof(GameServer).Assembly }))
			{
				try
				{
					area = (IGvGArea)script.CreateInstance(db.Type, false, BindingFlags.CreateInstance, null, new[] { db }, null, null);

					if (area != null)
						break;
				}
				catch (Exception e)
				{
					_log.Error("GvGManager._CreateArea error", e);
				}
			}
			if (area == null)
				_log.Fatal("GvGManager._CreateArea, can't create area id " + db.ID);

			return area;
		}

		#endregion

		public static bool TakeControl(GamePlayer player, int areaId, bool force = false)
		{
			return TakeControl(player, GetAreaByID(areaId), force);
		}

		public static bool TakeControl(GamePlayer player, IGvGArea area, bool force = false)
		{
			return area != null && area.TakeControl(player, force);
		}

		#region Utils
		private static IGvGArea GetAreaByID(int areaId)
		{
			IGvGArea area;
			_areas.TryGetValue(areaId, out area);
			return area;
		}
		#endregion
	}
}
