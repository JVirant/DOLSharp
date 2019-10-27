using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.GS.Scripts
{
	public class GuildCaptainGuard : AmteMob
	{
		private static readonly string[] _systemGuildIds = new[]
		{
			"063bbcc7-0005-4667-a9ba-402746c5ae15",
			"bdbc6f4a-b9f8-4316-b88b-9698e06cdd7b",
			"50d7af62-7142-4955-9f31-0c58ac1ac33f",
			"ce6f0b34-78bc-45a9-9f65-6e849d498f6c",
			"386c822f-996b-4db6-8bd8-121c07fc11cd"
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
			if (!base.Interact(player) || _guild == null)
				return false;
			if (player.Client.Account.PrivLevel == 1 && player.GuildID != _guild.GuildID)
				return false;

			if (player.Client.Account.PrivLevel == 1 &&  !player.GuildRank.Claim)
				player.Out.SendMessage($"Bonjour {player.Name}, je ne discute pas avec les bleus, circulez.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			player.Out.SendMessage($"Bonjour {player.GuildRank?.Title ?? ""} {player.Name}, que puis-je faire pour vous ?\n[modifier les alliances] [acheter un garde]\n", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text) || _guild == null)
				return false;
			if (!(source is GamePlayer player))
				return false;
			if (player.Client.Account.PrivLevel == 1 && (player.GuildID != _guild.GuildID || !player.GuildRank.Claim))
				return false;

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
					player.Out.SendMessage($"Vous devez prendre contact avec un Game Master d'Amtenaël.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
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
	}
}
