using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		 "&jailrp",
		 ePrivLevel.GM,
		 "Pour emprisonner un joueur de façon RP.",
		 "/jailrp <nom du joueur> pour relâcher un joueur (connecté ou non)",
        "/jailrp <nom du joueur> <raison> <amende en or> [heures] [jours] pour emprisonner un joueur")]
	[CmdAttribute(
		 "&jailhrp",
		 ePrivLevel.GM,
		 "Pour emprisonner un joueur de façon HRP.",
		 "/jailhrp <nom du joueur> pour relâcher un joueur (connecté ou non)",
		 "/jailhrp <nom du joueur> <raison> <heures> [jours] pour emprisonner un joueur")]
	[CmdAttribute(
		 "&jail", 
		 ePrivLevel.GM,
		 "Pour emprisonner un joueur de façon RP.",
		 "'/jail relache <nom du joueur>' Relâche le joueur s'il est en prison (RP ou HRP)",
         "'/jail rp <nom du joueur> <raison> <caution (en po)> <heure> [jours]' Met le joueur en prison RP",
         "'/jail hrp <nom du joueur> <raison> <heure> [jours]' Met le joueur en prison HRP",
		 "'/jail list [rp/hrp]' pour lister les prisonniers (RP ou HRP ou tous)")]
	public class JailCommandHandler : ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client, args);
				return;
			}
			
			switch(args[0].ToLower())
			{
				case "&jailrp":
					switch (args.Length)
					{
					    case 2:
					        args = new [] {"&jail", "relache", args[1]};
					        break;
					    case 5:
					        args = new [] {"&jail", "rp", args[1], args[2], args[3], args[4]};
					        break;
					    case 6:
					        args = new [] {"&jail", "rp", args[1], args[2], args[3], args[4], args[5]};
					        break;
					    default:
					        DisplaySyntax(client, args);
							return;
					}
					break;
					
				case "&jailhrp":
					switch (args.Length)
					{
					    case 2:
					        args = new [] {"jail", "relache", args[1]};
					        break;
					    case 4:
					        args = new [] {"jail", "hrp", args[1], args[2], args[3]};
					        break;
					    case 5:
					        args = new [] {"jail", "hrp", args[1], args[2], args[3], args[4]};
					        break;
					    default:
					        DisplaySyntax(client, args);
							return;
					}
					break;
			}
			
			switch(args[1].ToLower())
			{
				case "list":
					ListPrisoners(client, args);
					return;
					
				case "relache":
					if (args.Length == 3)
					{
						if (!JailMgr.Relacher(args[2]))
							client.Out.SendMessage("Joueur '" + args[2] + "' introuvable.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						else
							client.Out.SendMessage("Joueur '" + args[2] + "' relaché.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return;
					}
					DisplaySyntax(client, args);
					return;
					
				case "rp":
				case "hrp":
					Emprisonner(client, args);
					return;
			}
			
			DisplaySyntax(client, args);
		}
		
		private void Emprisonner(GameClient client, string[] args)
		{
			GamePlayer player = client.Player;

			if (args.Length <= 3)
			{
				DisplaySyntax(client, args);
				return;
			}

			Prisoner Pris = JailMgr.GetPrisoner(args[2]);
			if (Pris != null)
			{
				if (Pris.RP) player.Out.SendMessage("Le joueur '" + Pris.Name + "' est déjà en prison RP.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				else player.Out.SendMessage("Le joueur '" + Pris.Name + "' est déjà en prison HRP.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			bool Connected = true;
			bool RP = (args[1].ToLower() == "rp");

			GamePlayer Prisonnier = null;
			GameClient cli = WorldMgr.GetClientByPlayerName(args[2], false, false);
			if (cli == null)
				Connected = false;
			else
				Prisonnier = cli.Player;

			int cost = 0;
			if (RP)
			{
				try
				{
					cost = int.Parse(args[4]);
				}
				catch
				{
					player.Out.SendMessage("L'amende est incorrecte. (" + args[4] + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}

			DateTime sortie = DateTime.MinValue;
		    string raison = args[3];
			if (args.Length >= (RP ? 6 : 5))
			{
				try
				{
					int heures = int.Parse(args[(RP ? 5 : 4)]);
					int jours = heures / 24;
					heures = heures % 24;
					if (args.Length >= (RP ? 7 : 6))
					{
						try
						{
							jours += int.Parse(args[(RP ? 6 : 5)]);
						}
						catch
						{
							player.Out.SendMessage("Le nombre de jours est incorrect. (" + args[(RP ? 6 : 5)] + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
					}
					sortie = new DateTime(DateTime.Now.Ticks + (jours * 864000000000L) + (heures * 36000000000L));
				}
				catch
				{
					player.Out.SendMessage("Le nombre d'heures est incorrecte. (" + args[(RP ? 5 : 4)] + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}

			if (Connected)
			{
				if (RP)
				{
					JailMgr.EmprisonnerRP(Prisonnier, cost, sortie, player.Name, raison);
					player.Out.SendMessage(Prisonnier.Name + " a été emprisonné avec une amende de " + cost + "po.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
                    JailMgr.EmprisonnerHRP(Prisonnier, sortie, player.Name, raison);
					player.Out.SendMessage(Prisonnier.Name + " a été emprisonné.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
			else
			{
                if (RP && JailMgr.EmprisonnerRP(args[2], cost, sortie, player.Name, raison))
					player.Out.SendMessage(args[2] + " a été emprisonné avec une amende de " + cost + "po.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else if (!RP && JailMgr.EmprisonnerHRP(args[2], sortie, player.Name, raison))
					player.Out.SendMessage(args[2] + " a été emprisonné.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				else
					player.Out.SendMessage("Ce joueur (" + args[2] + ") n'a pas été trouvé.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
		
		private void ListPrisoners(GameClient client, string[] args)
		{
			string title;
			List<string> text = new List<string>();
			IList<Prisoner> prisonniers;

			if (args.Length == 3)
				switch (args[2].ToLower())
				{
					case "rp":
						title = "Prisonniers RP";
						prisonniers = GameServer.Database.SelectObjects<Prisoner>("RP = 1");
						break;
					case "hrp":
						title = "Prisonniers HRP";
						prisonniers = GameServer.Database.SelectObjects<Prisoner>("RP = 0");
						break;
					default:
						DisplaySyntax(client, args);
						return;
				}
			else
			{
				title = "Prisonniers RP et HRP";
				prisonniers = GameServer.Database.SelectAllObjects<Prisoner>();
			}
			if (prisonniers == null || prisonniers.Count <= 0)
				text.Add("Aucun prisonnier.");
			else
			{
				text.Add(prisonniers.Count + " prisonnier" + (prisonniers.Count == 1 ? " :" : "s :"));
				int i = 1;
				foreach (Prisoner pris in prisonniers)
					if (pris.Sortie.Ticks >= DateTime.Now.Ticks)
					{
					    text.Add(i + ". " + (pris.RP ? "RP " : "HRP") + " " + pris.Name + " - " + pris.Sortie.ToShortDateString() +
					             " " + pris.Sortie.ToShortTimeString() + "" + (pris.RP ? " - " + pris.Cost + "po" : "") + " " +
					             pris.Raison);
						i++;
					}
			}
			client.Out.SendCustomTextWindow(title, text);
		}

		public void DisplaySyntax(GameClient client, string[] args)
		{
			if (client == null || !client.IsPlaying)
				return;
			CmdAttribute[] attrib = (CmdAttribute[])GetType().GetCustomAttributes(typeof(CmdAttribute), false);
			if (attrib.Length == 0)
				return;

			foreach (CmdAttribute att in attrib)
			{
				if (att.Cmd == args[0])
				{
					client.Out.SendMessage(att.Description, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					foreach (string str in att.Usage)
						client.Out.SendMessage(str, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}
			return;
		}
	}
}
