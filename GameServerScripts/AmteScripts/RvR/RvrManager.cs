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
	public class RvrManager
	{
		private static readonly TimeSpan _startTime = new TimeSpan(20, 0, 0);
		private static readonly TimeSpan _endTime = new TimeSpan(23, 59, 59);
		private const int _checkInterval = 30 * 1000; // 30 seconds
		private static readonly GameLocation _stuckSpawn = new GameLocation("", 51, 434303, 493165, 3088, 1069);

		#region Static part
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static RvrManager _instance;
		private static RegionTimer _timer;

		public static RvrManager Instance { get { return _instance; } }

		[ScriptLoadedEvent]
		public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("RvRManger: Started");
			_instance = new RvrManager();
			_timer = new RegionTimer(WorldMgr.GetRegion(1).TimeManager)
			{
				Callback = _instance._CheckRvr
			};
			_timer.Start(10000);
		}

		[ScriptUnloadedEvent]
		public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("RvRManger: Stopped");
			_timer.Stop();
		}
		#endregion

		private bool _isOpen;
		private bool _isForcedOpen;
		private ushort _region;

		private readonly Guild _albion;
		private readonly Guild _midgard;
		private readonly Guild _hibernia;

		public bool IsOpen { get { return _isOpen; } }
		public ushort Region { get { return _region; } }

		/// <summary>
		/// &lt;regionID, Tuple&lt;TPs, spawnAlb, spawnMid, spawnHib&gt;&gt;
		/// </summary>
		private readonly Dictionary<ushort, Tuple<GameNPC[], GameLocation, GameLocation, GameLocation>> _maps =
			new Dictionary<ushort, Tuple<GameNPC[], GameLocation, GameLocation, GameLocation>>();

		private RvrManager()
		{
			_albion = GuildMgr.GetGuildByName("Albion");
			_midgard = GuildMgr.GetGuildByName("Midgard");
			_hibernia = GuildMgr.GetGuildByName("Hibernia");

			if (_albion == null)
				_albion = GuildMgr.CreateGuild(eRealm.Albion, "Albion");
			if (_midgard == null)
				_midgard = GuildMgr.CreateGuild(eRealm.Midgard, "Midgard");
			if (_hibernia == null)
				_hibernia = GuildMgr.CreateGuild(eRealm.Hibernia, "Hibernia");

			_albion.SaveIntoDatabase();
			_midgard.SaveIntoDatabase();
			_hibernia.SaveIntoDatabase();
			FindRvRMaps();
		}

		public IEnumerable<ushort> FindRvRMaps()
		{
			var npcs = WorldMgr.GetNPCsByGuild("RVR", eRealm.None);
			var maps = new List<ushort>();
			npcs.Foreach(n => { if (!maps.Contains(n.CurrentRegionID)) maps.Add(n.CurrentRegionID); });

			_maps.Clear();
			foreach (var id in maps)
			{
				var spawnAlb = WorldMgr.GetNPCsByNameFromRegion("ALB", id, eRealm.None).FirstOrDefault();
				var spawnMid = WorldMgr.GetNPCsByNameFromRegion("MID", id, eRealm.None).FirstOrDefault();
				var spawnHib = WorldMgr.GetNPCsByNameFromRegion("HIB", id, eRealm.None).FirstOrDefault();
				if (spawnAlb == null || spawnMid == null || spawnHib == null)
					continue;
				_maps.Add(id, new Tuple<GameNPC[], GameLocation, GameLocation, GameLocation>(new[] { spawnAlb, spawnMid, spawnHib },
					new GameLocation("Alb", spawnAlb),
					new GameLocation("Mid", spawnMid),
					new GameLocation("Hib", spawnHib)
					));
			}
			return (from m in _maps select m.Key);
		}

		private int _CheckRvr(RegionTimer callingtimer)
		{
			Console.WriteLine("Check RVR");
			if (!_isOpen)
			{
				_maps.Keys.Foreach(r => WorldMgr.GetClientsOfRegion(r).Foreach(RemovePlayer));
				if (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay < _endTime)
					Open(0, false);
			}
			else
			{
				WorldMgr.GetClientsOfRegion(_region).Where(cl => cl.Player.Guild == null).Foreach(cl => RemovePlayer(cl.Player));
				if (!_isForcedOpen && WorldMgr.GetClientsOfRegion(_region).Count < 3)
				{
					if ((DateTime.Now.TimeOfDay < _startTime || DateTime.Now.TimeOfDay > _endTime) && !Close())
						WorldMgr.GetClientsOfRegion(_region).Foreach(RemovePlayer);
				}
			}
			return _checkInterval;
		}

		private LordRvR[] _lords = new LordRvR[0];
		public LordRvR[] Lords => _lords.ToArray();

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

			_albion.RealmPoints = 0;
			_midgard.RealmPoints = 0;
			_hibernia.RealmPoints = 0;

			var reg = WorldMgr.GetRegion(_region);
			_lords = reg.Objects.Where(o => o != null && o is LordRvR).Select(o => (LordRvR)o).ToArray();
			foreach (var lord in _lords)
				lord.StartRvR();

			return true;
		}

		public bool Close()
		{
			if (!_isOpen)
				return false;
			_isOpen = false;
			_isForcedOpen = false;

			foreach (var lord in _lords)
				lord.StopRvR();

			WorldMgr.GetClientsOfRegion(_region).Where(player => player.Player != null).Foreach(RemovePlayer);
			GameServer.Database.SelectObjects<DOLCharacters>(c => c.Region == _region).Foreach(RemovePlayer);
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
				player.Guild.RemovePlayer("RVR", player);

			switch (player.Realm)
			{
				case eRealm.Albion:
					_albion.AddPlayer(player);
					player.MoveTo(_maps[_region].Item2);
					player.Bind(true);
					break;

				case eRealm.Midgard:
					_midgard.AddPlayer(player);
					player.MoveTo(_maps[_region].Item3);
					player.Bind(true);
					break;

				case eRealm.Hibernia:
					_hibernia.AddPlayer(player);
					player.MoveTo(_maps[_region].Item4);
					player.Bind(true);
					break;
			}

			if (player.Guild != null)
				foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
					i.Emblem = player.Guild.Emblem;

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
			var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == player.InternalID);
			if (rvr == null)
			{
				player.MoveTo(_stuckSpawn);
				if (player.Guild != null && (player.Guild == _albion || player.Guild == _midgard || player.Guild == _hibernia))
					player.Guild.RemovePlayer("RVR", player);
				player.SaveIntoDatabase();
			}
			else
			{
				rvr.ResetCharacter(player);
				player.MoveTo((ushort)rvr.OldRegion, rvr.OldX, rvr.OldY, rvr.OldZ, (ushort)rvr.OldHeading);
				if (player.Guild != null)
					player.Guild.RemovePlayer("RVR", player);
				if (!string.IsNullOrWhiteSpace(rvr.GuildID))
				{
					var guild = GuildMgr.GetGuildByGuildID(rvr.GuildID);
					if (guild != null)
					{
						guild.AddPlayer(player, guild.GetRankByID(rvr.GuildRank));

						foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
							i.Emblem = guild.Emblem;
					}
				}
				player.SaveIntoDatabase();
				GameServer.Database.DeleteObject(rvr);
			}
		}

		public void RemovePlayer(DOLCharacters ch)
		{
			var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == ch.ObjectId);
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


		private IList<string> _statCache = new List<string>();
		private DateTime _statLastCacheUpdate = DateTime.Now;

		public IList<string> GetStatistics()
		{
			if (DateTime.Now.Subtract(_statLastCacheUpdate) >= new TimeSpan(0, 0, 5))
			{
				_statLastCacheUpdate = DateTime.Now;
				var clients = WorldMgr.GetClientsOfRegion(_region);
				var albCount = clients.Where(c => c.Player.Realm == eRealm.Albion).Count();
				var midCount = clients.Where(c => c.Player.Realm == eRealm.Midgard).Count();
				var hibCount = clients.Where(c => c.Player.Realm == eRealm.Hibernia).Count();

				_statCache = new List<string>
					{
						"Statistiques du RvR:",
						" - Albion: ",
						albCount + " joueurs",
						_albion.RealmPoints + " PR",
						" - Midgard: ",
						midCount + " joueurs",
						_midgard.RealmPoints + " PR",
						" - Hibernia: ",
						hibCount + " joueurs",
						_hibernia.RealmPoints + " PR",
						"",
						" - Total: ",
						clients.Count + " joueurs",
						(_albion.RealmPoints + _midgard.RealmPoints + _hibernia.RealmPoints) + " PR",
						"",
						string.Join("\n", _lords.Select(l => l.GetScores())),
						"",
						"Le rvr est " + (_isOpen ? "ouvert" : "fermé") + ".",
						"(Mise à jour toutes les 5 secondes)",
					};
			}
			return _statCache;
		}

		public bool IsInRvr(GameLiving obj)
		{
			return IsOpen && obj != null && obj.CurrentRegionID == _region;
		}

		public bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
		{
			if (attacker.Realm == defender.Realm)
			{
				if (!quiet)
					_MessageToLiving(attacker, "Vous ne pouvez pas attaquer un membre de votre royaume !");
				return false;
			}
			return true;
		}

		public bool IsRvRRegion(ushort id)
		{
			return _maps.ContainsKey(id);
		}

		private static void _MessageToLiving(GameLiving living, string message)
		{
			if (living is GamePlayer)
				((GamePlayer)living).Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
	}
}
