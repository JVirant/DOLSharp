using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amte;
using AmteScripts.Utils;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;

namespace AmteScripts.Managers
{
	public class PvpManager
	{
		private static readonly TimeSpan _startTime = new TimeSpan(20, 0, 0);
		private static readonly TimeSpan _endTime = new TimeSpan(23, 59, 59);
		private const int _checkInterval = 30 * 1000; // 30 seconds
		private static readonly GameLocation _stuckSpawn = new GameLocation("", 51, 434303, 493165, 3088, 1069);

		#region Static part
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static PvpManager _instance;
		private static RegionTimer _timer;

		public static PvpManager Instance { get { return _instance; } }

		[ScriptLoadedEvent]
		public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("PvP Manager: Started");
			_instance = new PvpManager();
			_timer = new RegionTimer(WorldMgr.GetRegion(1).TimeManager)
			{
				Callback = _instance._CheckPvP
			};
			_timer.Start(10000);
		}

		[ScriptUnloadedEvent]
		public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("PvP Manager: Stopped");
			_timer.Stop();
		}
		#endregion

		private bool _isOpen;
		private bool _isForcedOpen;
		private ushort _region;

		public bool IsOpen { get { return _isOpen; } }
		public ushort Region { get { return _region; } }

		/// <summary>
		/// &lt;regionID, Tuple&lt;TPs, spawnAlb, spawnMid, spawnHib&gt;&gt;
		/// </summary>
		private readonly Dictionary<ushort, Tuple<GameNPC, GameLocation>> _maps = new Dictionary<ushort, Tuple<GameNPC, GameLocation>>();

		private PvpManager()
		{
			FindPvPMaps();
		}

		public IEnumerable<ushort> FindPvPMaps()
		{
			var npcs = WorldMgr.GetNPCsByGuild("PVP", eRealm.None);
			var maps = npcs.Select(n => n.CurrentRegionID).Distinct();

			_maps.Clear();
			foreach (var id in maps)
			{
				var spawn = WorldMgr.GetNPCsByNameFromRegion("SPAWN", id, eRealm.None).FirstOrDefault();
				if (spawn == null)
					continue;
				_maps.Add(id, new Tuple<GameNPC, GameLocation>(spawn, new GameLocation("Spawn", spawn.CurrentRegionID, spawn.X, spawn.Y, spawn.Z, spawn.Heading)));
			}
			return (from m in _maps select m.Key);
		}

		private int _CheckPvP(RegionTimer callingtimer)
		{
			Console.WriteLine("Check PVP");
			if (!_isOpen)
			{
				_maps.Keys.ForEach(r => WorldMgr.GetClientsOfRegion(r).Foreach(RemovePlayer));
				if (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay < _endTime)
					Open(0, false);
			}
			else if (!_isForcedOpen && WorldMgr.GetClientsOfRegion(_region).Count < 3)
			{
				if ((DateTime.Now.TimeOfDay < _startTime || DateTime.Now.TimeOfDay > _endTime) && !Close())
					WorldMgr.GetClientsOfRegion(_region).Foreach(RemovePlayer);
			}
			return _checkInterval;
		}

		public bool Open(ushort region, bool force)
		{
			_isForcedOpen = force;
			if (_isOpen)
				return true;
			_isOpen = true;
			if (region == 0)
				_region = _maps.Keys.ElementAt(Util.Random(_maps.Count - 1));
			else if (!_maps.ContainsKey(region))
				return false;
			else
				_region = region;
			return true;
		}

		public bool Close()
		{
			if (!_isOpen)
				return false;
			_isOpen = false;
			_isForcedOpen = false;

			WorldMgr.GetClientsOfRegion(_region).Where(player => player.Player != null).Foreach(RemovePlayer);
			GameServer.Database.SelectObjects<DOLCharacters>("Region = " + _region).Foreach(RemovePlayer);
			return true;
		}

		public bool AddPlayer(GamePlayer player)
		{
			if (!_isOpen || player.Level < 20)
				return false;
			if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
			{
				player.Out.SendMessage("Casse-toi connard de GM !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}
			RvrPlayer rvr = new RvrPlayer(player);
			GameServer.Database.AddObject(rvr);

			if (player.Guild != null)
				player.Guild.RemovePlayer("PVP", player);

			player.MoveTo(_maps[_region].Item2);
			player.Bind(true);
			return true;
		}


		public void RemovePlayer(GameClient client)
		{
			if (client.Player != null)
				RemovePlayer(client.Player);
		}

		public void RemovePlayer(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
				return;
			var rvr = GameServer.Database.SelectObject<RvrPlayer>("PlayerID = '" + GameServer.Database.Escape(player.InternalID) + "'");
			if (rvr == null)
			{
				player.MoveTo(_stuckSpawn);
				player.SaveIntoDatabase();
			}
			else
			{
				rvr.ResetCharacter(player);
				player.MoveTo((ushort)rvr.OldRegion, rvr.OldX, rvr.OldY, rvr.OldZ, (ushort)rvr.OldHeading);
				if (player.Guild != null)
					player.Guild.RemovePlayer("PVP", player);
				if (!string.IsNullOrWhiteSpace(rvr.GuildID))
				{
					var guild = GuildMgr.GetGuildByGuildID(rvr.GuildID);
					if (guild != null)
						guild.AddPlayer(player, guild.GetRankByID(rvr.GuildRank));
				}
				player.SaveIntoDatabase();
				GameServer.Database.DeleteObject(rvr);
			}
		}

		public void RemovePlayer(DOLCharacters ch)
		{
			var rvr = GameServer.Database.SelectObject<RvrPlayer>("PlayerID = '" + GameServer.Database.Escape(ch.ObjectId) + "'");
			if (rvr == null)
			{
				// AHHHHHHHHHHH
			}
			else
			{
				rvr.ResetCharacter(ch);
				GameServer.Database.SaveObject(ch);
				GameServer.Database.DeleteObject(rvr);
			}
		}

		public bool IsIn(GameLiving obj)
		{
			return IsOpen && obj != null && obj.CurrentRegionID == _region;
		}

		public bool IsPvPRegion(ushort id)
		{
			return _maps.ContainsKey(id);
		}
	}
}
