using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.GS.Scripts
{
	public class GuildCaptainGuard : AmteMob
	{
		public const long CLAIM_COST = 500 * 100 * 100; // 500g
		public const ushort AREA_RADIUS = 4096;
		public const ushort NEUTRAL_EMBLEM = 256;

		/// <summary>
		/// "Fausses" guildes : Albion, Hibernia, Midgard, Les Maitres du Temps, Citoyens d'Amtenael
		/// </summary>
		private static readonly string[] _systemGuildIds = new[]
		{
			"063bbcc7-0005-4667-a9ba-402746c5ae15",
			"bdbc6f4a-b9f8-4316-b88b-9698e06cdd7b",
			"50d7af62-7142-4955-9f31-0c58ac1ac33f",
			"ce6f0b34-78bc-45a9-9f65-6e849d498f6c",
			"386c822f-996b-4db6-8bd8-121c07fc11cd",
		};
		public static readonly List<GuildCaptainGuard> allCaptains = new List<GuildCaptainGuard>();

		private Guild _guild;

		public List<string> safeGuildIds = new List<string>();
		private readonly AmteCustomParam _safeGuildParam;

		public GuildCaptainGuard()
		{
			_safeGuildParam = new AmteCustomParam(
				"safeGuildIds",
				() => string.Join(";", safeGuildIds),
				v => safeGuildIds = v.Split(';').ToList(),
				"");
		}

		public GuildCaptainGuard(INpcTemplate npc)
			: base(npc)
		{
			_safeGuildParam = new AmteCustomParam(
				"safeGuildIds",
				() => string.Join(";", safeGuildIds),
				v => safeGuildIds = v.Split(';').ToList(),
				"");
		}

		public override AmteCustomParam GetCustomParam()
		{
			var param = base.GetCustomParam();
			param.next = _safeGuildParam;
			return param;
		}

		public override string GuildName {
			get => base.GuildName;
			set {
				base.GuildName = value;
				_guild = GuildMgr.GetGuildByName(value);
				ResetArea(_guild?.Emblem ?? NEUTRAL_EMBLEM);
			}
		}

		public override bool AddToWorld()
		{
			var r = base.AddToWorld();
			_guild = GuildMgr.GetGuildByName(GuildName);
			allCaptains.Add(this);
			return r;
		}

		public override bool RemoveFromWorld()
		{
			allCaptains.Remove(this);
			return base.RemoveFromWorld();
		}

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player) || player.Guild == null)
				return false;

			if (player.Client.Account.PrivLevel == 1 && !player.GuildRank.Claim)
			{
				player.Out.SendMessage($"Bonjour {player.Name}, je ne discute pas avec les bleus, circulez.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			if (player.GuildID != _guild?.GuildID)
			{
				player.Out.SendMessage(
					$"Bonjour {player.GuildRank?.Title ?? ""} {player.Name} que puis-je faire pour vous ?\n[capturer le territoire] ({Money.GetShortString(CLAIM_COST)})",
					eChatType.CT_System,
					eChatLoc.CL_PopupWindow
				);
				return true;
			}

			player.Out.SendMessage($"Bonjour {player.GuildRank?.Title ?? ""} {player.Name}, que puis-je faire pour vous ?\n\n[modifier les alliances]\n", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text) || _guild == null)
				return false;
			if (!(source is GamePlayer player))
				return false;
			if (player.GuildID != _guild.GuildID)
			{
				if (player.GuildRank.Claim && text == "capturer le territoire")
				{
					Claim(player);
					return true;
				}
				if (player.Client.Account.PrivLevel == 1)
					return false;
			}

			switch(text)
			{
				case "default":
				case "modifier les alliances":
					var guilds = GuildMgr.GetAllGuilds()
						.Where(g => !_systemGuildIds.Contains(g.GuildID) && g.GuildID != _guild.GuildID)
						.OrderBy(g => g.Name)
						.Select(g => {
							var safe = safeGuildIds.Contains(g.GuildID);
							if (safe)
								return $"{g.Name}: [{g.ID}. attaquer à vue]";
							return $"{g.Name}: [{g.ID}. ne plus attaquer à vue]";
						})
						.Aggregate((a, b) => $"{a}\n{b}");
					var safeNoGuild = safeGuildIds.Contains("NOGUILD");
					guilds += "\nLes sans guildes: [256. ";
					guilds += (safeNoGuild ? "" : "ne plus ") + "attaquer à vue]";
					player.Out.SendMessage($"Voici la liste des guildes et leurs paramètres :\n${guilds}", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					return true;
				case "acheter un garde":
					BuyGuard(player);
					return true;
			}

			var dotIdx = text.IndexOf('.');
			ushort id;
			if (dotIdx > 0 && ushort.TryParse(text.Substring(0, dotIdx), out id))
			{
				var guild = GuildMgr.GetAllGuilds().FirstOrDefault(g => g.ID == id);
				if (guild == null && id != 256)
					return false;
				var guildID = guild == null ? "NOGUILD" : guild.GuildID;
				if (safeGuildIds.Contains(guildID))
					safeGuildIds.Remove(guildID);
				else
					safeGuildIds.Add(guildID);
				SaveIntoDatabase();
				return WhisperReceive(source, "default");
			}
			return false;
		}

		public IEnumerable<SimpleGvGGuard> GetGuardsInRadius(ushort radius = AREA_RADIUS)
		{
			foreach (var npc in GetNPCsInRadius(radius).OfType<SimpleGvGGuard>())
			{
				if (npc.Captain != this)
					continue;
				yield return npc;
			}
		}

		public void ResetArea(int newemblem, int oldemblem = NEUTRAL_EMBLEM)
		{
			foreach (var guard in GetGuardsInRadius())
				guard.Captain = this;
			foreach (var obj in GetItemsInRadius(AREA_RADIUS))
				if (obj is GameStaticItem item && item.Emblem == oldemblem)
					item.Emblem = newemblem;
		}

		public void BuyGuard(GamePlayer player)
		{
			player.Out.SendMessage($"Vous devez prendre contact avec un Game Master d'Amtenaël.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}

		public void Claim(GamePlayer player)
		{
			if (!Name.StartsWith("Capitaine"))
			{
				player.Out.SendMessage(
					"Vous devez demander à un GM pour ce type de territoire.",
					eChatType.CT_System,
					eChatLoc.CL_PopupWindow
				);
				return;
			}

			if (DateTime.Now.DayOfWeek != DayOfWeek.Monday || DateTime.Now.Hour < 21 || DateTime.Now.Hour > 23)
			{
				player.Out.SendMessage(
					"Il n'est pas possible de capturer des territoires aujourd'hui à cette heure-ci.\n" +
					"Pour le moment, les territoires ne sont prenables que le lundi entre 21h et 23h.\n",
					eChatType.CT_System,
					eChatLoc.CL_PopupWindow
				);
				return;
			}

			if (GetGuardsInRadius(AREA_RADIUS).Any(g => g.IsAlive))
			{
				player.Out.SendMessage(
					"Vous devez tuer tous les gardes avant de pouvoir prendre possession du territoire.",
					eChatType.CT_System,
					eChatLoc.CL_PopupWindow
				);
				return;
			}

			if (!player.RemoveMoney(CLAIM_COST))
			{
				player.Out.SendMessage(
					"Vous n'avez pas assez d'argent pour prendre possession du territoire.",
					eChatType.CT_System,
					eChatLoc.CL_PopupWindow
				);
				return;
			}

			var oldguild = GuildMgr.GetGuildByName(GuildName);
			GuildName = player.GuildName;
			SaveIntoDatabase();
			ResetArea(player.Guild?.Emblem ?? NEUTRAL_EMBLEM, oldguild?.Emblem ?? NEUTRAL_EMBLEM);
			player.Out.SendMessage(
				"Le territoire appartient maintenant à votre guilde, que voulez-vous faire ?\n\n[modifier les alliances]\n",
				eChatType.CT_System,
				eChatLoc.CL_PopupWindow
			);
		}
	}
}
