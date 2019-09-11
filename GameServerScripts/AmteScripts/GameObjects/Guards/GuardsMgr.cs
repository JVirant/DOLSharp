using AmteScripts.Managers;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AmteScripts.Managers
{
	class GuardsMgr
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Dictionary<string, GuardXGuild> _attackGuildIDs = new Dictionary<string, GuardXGuild>();

		public static Dictionary<string, int> AttackGuildIDs
		{
			get
			{
				return _attackGuildIDs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Aggro);
			}
		}

		[GameServerStartedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			_log.Info("GuardsMgr initialized: " + _Load());
		}

		private static bool _Load()
		{
			var gxgs = GameServer.Database.SelectAllObjects<GuardXGuild>();
			foreach (var gxg in gxgs)
				_attackGuildIDs.Add(gxg.GuildID, gxg);
			return true;
		}

		public static int CalculateAggro(AmtePlayer player)
		{
			int aggro = 0;
			if (!string.IsNullOrWhiteSpace(player.GuildID))
			{
				GuardXGuild gxg;
				if (_attackGuildIDs.TryGetValue(player.GuildID, out gxg))
					aggro += gxg.Aggro;
			}
			aggro -= 100;
			return aggro.Clamp(0, 100);
		}

		public static void AddAggro(Guild guild, int amount)
		{
			GuardXGuild gxg;
			if (_attackGuildIDs.TryGetValue(guild.GuildID, out gxg))
				gxg.Aggro += amount;
			else
			{
				gxg = new GuardXGuild();
				gxg.Aggro = amount;
				gxg.GuildID = guild.GuildID;
				_attackGuildIDs.Add(gxg.GuildID, gxg);
			}
			if (gxg.IsPersisted)
				GameServer.Database.SaveObject(gxg);
			else
				GameServer.Database.AddObject(gxg);
		}
	}
}

namespace DOL.Database
{
	[DataTable(TableName = "GuardXGuild")]
	public class GuardXGuild : DataObject
	{
		[PrimaryKey(AutoIncrement = true)]
		public int ID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string GuildID { get; set; }

		[DataElement(AllowDbNull = false)]
		public int Aggro { get; set; }

		public GuardXGuild()
		{
			GuildID = "";
			Aggro = 0;
		}
	}

	[DataTable(TableName = "GuardXPlayer")]
	public class GuardXPlayer : DataObject
	{
		[PrimaryKey(AutoIncrement = true)]
		public int ID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string PlayerID { get; set; }

		[DataElement(AllowDbNull = false)]
		public int Aggro { get; set; }

		public GuardXPlayer()
		{
			PlayerID = "";
			Aggro = 0;
		}
	}
}

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&guard",
		ePrivLevel.GM,
		"Gérer les gardes",
		"/guard guildaggro add <amount>, ajoute de l'aggro (>100 pour que ça attaque)",
		"/guard guildaggro sub <amount>, retire de l'aggro (<0 pour aider le joueur)",
		"/guard listaggro"
	)]
	public class GuardCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			switch(args[1].ToLower())
			{
				case "guildaggro": this._guildaggro(client, args); return;
				case "listaggro": this._listaggro(client); return;
			}
			DisplaySyntax(client);
		}

		public void _guildaggro(GameClient client, string[] args)
		{
			if (args.Length != 4)
			{
				DisplaySyntax(client);
				return;
			}
			if (!(client.Player.TargetObject is AmtePlayer) || ((AmtePlayer)client.Player.TargetObject).Guild == null)
			{
				client.Out.SendMessage("Vous devez cibler un joueur avec une guilde.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			var target = (AmtePlayer)client.Player.TargetObject;
			var amount = int.Parse(args[3]);
			switch (args[2].ToLower())
			{
				case "add": break;
				case "sub":
				case "substract":
				case "remove": amount = -amount; break;
			}
			GuardsMgr.AddAggro(target.Guild, amount);
		}

		public void _listaggro(GameClient client)
		{
			client.Out.SendMessage("Guilde: niveau d'aggro", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			foreach (var kvp in GuardsMgr.AttackGuildIDs)
			{
				var guild = GuildMgr.GetGuildByGuildID(kvp.Key);
				client.Out.SendMessage(guild.Name + ": " + kvp.Value, eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
