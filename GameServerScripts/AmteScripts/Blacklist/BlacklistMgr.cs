using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Scripts
{
	public static class BlacklistMgr
	{
		public static float RemoveReputation = 1.0f / 4; // 1pt toutes les 4heures (en comptant 3h de jeu/jour)
		public static float ReputationForBlacklist = 9.6f; // Réputation minimum avant d'être dans la BL
		public static float ReportingCost = 1; // Nb point donné (cf ratio pour les autres trucs)
		public static float ReportingGroupRatio = 0.5f; // Ratio des membres du groupe
		public static float ReportingGroupMembers = 0.05f; // Retire 0.05/membre du groupe au ratio de groupe
		public static float ReputationDeathRatio = 0.5f; // Ratio retiré par meurtre d'un BL
		public static float ReputationDeathGuardRatio = 0.5f; // Ratio retiré par tête donné aux gardes
		public static float ReputationBLDeathCost = 0.1f; // Nb point retiré par mort pvp d'un BL
		public static float KillInGuardSight = 0.2f; // Nb point ajouté par kill à la vue d'un garde
		public static TimeSpan MaxTimeToReport = new TimeSpan(0, 10, 0);
		public static TimeSpan LastReportTime = new TimeSpan(1, 0, 0); // Laps de temps dans lequel les derniers signalements sont pris en compte

		public static float MinReputation = -5;
		public static float MaxReputation = 20;

		public static ItemTemplate HeadTemplate;

		public static GameLocation Bind = new GameLocation("Bind", 51, 395117, 416493, 4172, 3053);

		#region Event/timer part
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static Timer _timer;

		[GameServerStartedEvent]
		public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("BlacklistMgr: Started");
			HeadTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>("head_blacklist") ?? new ItemTemplate();
			_timer = new Timer(o => _DoManyThings(), null, 1, 10 * 60 * 1000); // 10min
		}

		[GameServerStoppedEvent]
		public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
		{
			log.Info("BlacklistMgr: Stopped");
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		private static void _DoManyThings()
		{
			log.Info("BlacklistMgr: reputation changing...");
			foreach (var player in WorldMgr.GetAllPlayingClients().Where(c => c.Player != null).Select(c => c.Player).OfType<AmtePlayer>())
			{
				if (player.LastPosition == player && DateTime.Now - player.LastActivity > new TimeSpan(0, 10, 0))
					continue;
				if (player.LastPosition != player)
				{
					player.LastActivity = DateTime.Now;
					player.LastPosition = new Point3D(player);
				}

				lock (player.Blacklist)
				{
					player.Blacklist.Reputation = (player.Blacklist.Reputation - RemoveReputation/6).Clamp(MinReputation, MaxReputation);
					player.Blacklist.Dirty = true;
				}
			}
			log.Info("BlacklistMgr: reputation changed !");
		}
		#endregion

		#region Utils
		private static BlacklistPlayer _GetBlacklist(string playerID, bool create = true)
		{
			var bl = GameServer.Database.FindObjectByKey<BlacklistPlayer>(playerID);
			if (bl == null && create)
				bl = new BlacklistPlayer { PlayerID = playerID };
			return bl;
		}

		/// <summary>
		/// Le joueur est blacklisté ?
		/// </summary>
		public static bool IsBlacklisted(AmtePlayer player)
		{
			return player.Blacklist.Reputation >= ReputationForBlacklist;
		}
		#endregion

		#region AddReput / GuardsCheck
		private static void AddReputation(AmtePlayer player, BlacklistPlayer bl, float amount)
		{
			var newValue = (bl.Reputation + amount).Clamp(MinReputation, MaxReputation);
			var diff = newValue - bl.Reputation;
			bl.Reputation = newValue;
			bl.Save();

			if (player == null)
				return;

			if (diff > 0.1)
				player.SendMessage("Votre mauvaise réputation augmente !");
			else if (diff < 0.1)
				player.SendMessage("Votre mauvaise réputation diminue !");

			if (newValue < ReputationForBlacklist)
				return;
			player.BindRegion = Bind.RegionID;
			player.BindHeading = Bind.Heading;
			player.BindXpos = Bind.X;
			player.BindYpos = Bind.Y;
            player.BindZpos = Bind.Z;
            player.SaveIntoDatabase();
		}

		private static void GuardsCheck(AmtePlayer victim, AmtePlayer killer)
		{
			if (!victim.GetNPCsInRadius(WorldMgr.YELL_DISTANCE).OfType<IGuardNPC>().Any() &&
				!killer.GetNPCsInRadius(WorldMgr.YELL_DISTANCE).OfType<IGuardNPC>().Any())
				return;

			lock (killer.Blacklist)
				AddReputation(killer, killer.Blacklist, KillInGuardSight);
			BlacklistLog.Add(killer, killer.Name, KillInGuardSight, "Meurtre de " + victim.Name + " devant un garde");
		}
		#endregion

		#region Reports and kills
		/// <summary>
		/// Lorqu'on signale un joueur
		/// </summary>
		public static bool ReportPlayer(AmtePlayer victim)
		{
			if (victim.LastKillerID == null || victim.LastDeath < DateTime.Now.Subtract(MaxTimeToReport))
				return false;

			var killerID = victim.LastKillerID;
			var killer = WorldMgr.GetAllPlayingClients().Where(c => c.Player.InternalID == killerID).Select(c => c.Player).OfType<AmtePlayer>().FirstOrDefault();

			float victimReput;
			var vBl = victim.Blacklist;
			lock (vBl)
			{
				victimReput = vBl.Reputation;
				vBl.HasReported++;
				vBl.Save();
			}

			var val = CalcReportingCost(victim.InternalID, killerID) / (1 + (float)Math.Log(Math.Max(1, victimReput), 6));

			var kBl = killer == null ? _GetBlacklist(killerID) : killer.Blacklist;
			lock (kBl)
			{
				kBl.BeReported++;
				AddReputation(killer, kBl, val);
			}

			BlacklistLog.Add(killer, kBl.PlayerName, val, "Signalé par " + victim.Name);

			if (killer != null && killer.Group != null)
			{
				val = val * (ReportingGroupRatio - ReportingGroupMembers * killer.Group.MemberCount);
				foreach (var pl in killer.Group.GetPlayersInTheGroup().OfType<AmtePlayer>().Where(p => p != killer))
				{
					lock (pl.Blacklist)
					{
						pl.Blacklist.BeReported++;
						AddReputation(pl, pl.Blacklist, val);
					}
					BlacklistLog.Add(pl, pl.Name, val, "Signalé par " + victim.Name + " (groupe)");
				}
			}
			victim.LastKillerID = null;
			return true;
		}

		/// <summary>
		/// Lorsqu'on ramène une tête au garde
		/// </summary>
		public static void GuardReportBL(AmtePlayer killer, string victimID)
		{
			var victim = WorldMgr.GetAllPlayingClients().Where(c => c.Player.InternalID == victimID).Select(c => c.Player).OfType<AmtePlayer>().FirstOrDefault();

			var vBl = victim == null ? _GetBlacklist(victimID) : victim.Blacklist;
			float val;
			lock (vBl)
				val = ReportingCost / (1 + (float)Math.Log(Math.Max(1, vBl.Reputation), 6));
			val *= ReputationDeathGuardRatio;

			var kBl = killer.Blacklist;
			lock (kBl)
				AddReputation(killer, kBl, -val);
			BlacklistLog.Add(killer, killer.Name, -val, "Tête de " + vBl.PlayerName + " donné aux gardes");
		}

		/// <summary>
		/// Lorsqu'on tue un blacklisté
		/// </summary>
		public static void KillBlacklisted(AmtePlayer victim, AmtePlayer killer)
		{
			var vBl = victim.Blacklist;

			float val;
			lock(vBl)
				val = ReportingCost / (1 + (float)Math.Log(Math.Max(1, vBl.Reputation), 6));
			val *= ReputationDeathRatio;

			var bl = killer.Blacklist;
			lock (bl)
			{
				bl.KilledBlacklisted++;
				AddReputation(killer, bl, -val);
			}
			BlacklistLog.Add(killer, killer.Name, -val, "Meurtre de " + victim.Name);

			bl = victim.Blacklist;
			lock (bl)
				AddReputation(victim, bl, -ReputationBLDeathCost);
			BlacklistLog.Add(victim, victim.Name, -ReputationBLDeathCost, "Tué par " + killer.Name);
		}

		/// <summary>
		/// Un joueur qui tue un autre joueur
		/// </summary>
		public static void PlayerKilledByPlayer(AmtePlayer victim, AmtePlayer killer)
		{
			if (victim == killer)
				return;
			var inBL = IsBlacklisted(victim);
			ItemUnique iu = new ItemUnique(HeadTemplate)
				{
					Name = "Tête de " + victim.Name,
					MessageArticle = victim.InternalID,
					CanDropAsLoot = inBL,
					MaxCondition = (int)DateTime.Now.Subtract(new DateTime(2000, 1, 1)).TotalSeconds
				};
			GameServer.Database.AddObject(iu);
			if (killer.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(iu)))
				killer.SendMessage("Vous avez récupérer la tête de " + victim.Name + ".", eChatType.CT_Loot);
			else
				killer.SendMessage("Vous n'avez pas pu récupérer la tête de " + victim.Name + ", votre inventaire est plein !", eChatType.CT_Loot);

			if (inBL)
				KillBlacklisted(victim, killer);
			GuardsCheck(victim, killer);
		}
		#endregion

		#region Calcs
		private static readonly Dictionary<string, List<Tuple<DateTime, string>>> _playerXdateAndKiller = new Dictionary<string, List<Tuple<DateTime, string>>>();

		private static float CalcReportingCost(string victim, string killer)
		{
			List<Tuple<DateTime, string>> list;
			var date = DateTime.Now - new TimeSpan(1, 0, 0);
			if (!_playerXdateAndKiller.TryGetValue(victim, out list))
				_playerXdateAndKiller.Add(victim, list = new List<Tuple<DateTime, string>>());
			else
				list.RemoveAll(t => t.Item1 < date);
			list.Add(new Tuple<DateTime, string>(DateTime.Now, killer));
			return ReportingCost/list.Where(t => t.Item1 >= date).Count();
		}
		#endregion

		#region Infos
		public static List<string> GetInfos(AmtePlayer player)
		{
			var bl = player.Blacklist;
			return new List<string>
				{
					player.Name,
					"Réputation: " + bl.Reputation,
					"A signaler: " + bl.HasReported + " morts",
					"A été signalé: " + bl.BeReported + " fois",
					"A tué: " + bl.KilledBlacklisted + " personnes sur la liste"
				};
		}

		public static List<string> GetBlacklistedNames()
		{
			var data = GameServer.Database.SelectObjects<BlacklistPlayer>(
				"Reputation > '" + GameServer.Database.Escape(ReputationForBlacklist.ToString().Replace(',', '.')) + "'");

			return data.OrderBy(p => p.PlayerName).Select(p => p.PlayerName).ToList();
		}
		#endregion
	}
}
