using System.Collections.Generic;
using System.Linq;
using DOL.Database;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&casier",
		ePrivLevel.Player,
		"Gestion des casiers",
		"'/casier info [account]' Affiche le casier du joueur selectionné ou du compte indiqué",
		"'/casier add <account> <raison>' Ajoute une entrée public",
		"'/casier staff <account> <raison>' Ajoute une entrée privé",
		"'/casier padd <player> <raison>' Ajoute une entrée public",
		"'/casier pstaff <player> <raison>' Ajoute une entrée privé")]
	public class CasierCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
			if (client.Account.PrivLevel <= (uint)ePrivLevel.Player)
			{
				var db =
					GameServer.Database.SelectObjects<Casier>("AccountName = '" + GameServer.Database.Escape(client.Account.Name) + "'");
				var text = new List<string>();
				db.Where(i => !i.StaffOnly).OrderBy(i => i.Date).Foreach(
					i => text.Add(i.Date.ToShortDateString() + " " + i.Date.ToShortTimeString() + " - " + i.Reason));
				client.Out.SendCustomTextWindow("Votre casier", text);
				return;
			}

        	if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

        	if (args.Length > 4)
        		args = new[] {args[0], args[1], args[2], string.Join(" ", args, 3, args.Length - 3)};

        	bool staffOnly = args[1].ToLower().EndsWith("staff");
			Casier casier;

        	switch (args[1].ToLower())
        	{
        		case "info":
        			if (args.Length == 2)
        			{
        				if (!(client.Player.TargetObject is GamePlayer))
        				{
        					DisplaySyntax(client);
        					return;
        				}
        				args = new[] {args[0], args[1], ((GamePlayer) client.Player.TargetObject).Client.Account.Name};
        			}

        			var db =
        				GameServer.Database.SelectObjects<Casier>("AccountName = '" + GameServer.Database.Escape(args[2].ToLower()) +
        				                                          "'").OrderBy(i => i.Date);
        			var text = new List<string>();
        			text.Add("Public:");
        			db.Where(i => !i.StaffOnly).Foreach(
        				i => text.Add(i.Date.ToShortDateString() + " " + i.Date.ToShortTimeString() + " - " + i.Author + ": " + i.Reason));
        			text.Add("");
        			text.Add("Staff:");
        			db.Where(i => i.StaffOnly).Foreach(
						i => text.Add(i.Date.ToShortDateString() + " " + i.Date.ToShortTimeString() + " - " + i.Author + ": " + i.Reason));
        			client.Out.SendCustomTextWindow("Casier de " + args[2], text);
        			break;

        		case "staff":
        		case "add":
					if (GameServer.Database.SelectObject<Account>("Name = '" + GameServer.Database.Escape(args[2].ToLower()) +"'") == null)
					{
						DisplayMessage(client, "Le compte \"{0}\" est introuvable.", args[2]);
						break;
					}

        			GameServer.Database.AddObject(casier = new Casier(client.Account.Name, args[2].ToLower(), args[3], staffOnly));
					DisplayMessage(client, "Ajouté: " + casier.Date.ToShortDateString() + " " + casier.Date.ToShortTimeString() + ": " + casier.Reason);
        			break;

				case "pstaff":
				case "padd":
        			var ch = GameServer.Database.SelectObject<DOLCharacters>("Name LIKE '" + GameServer.Database.Escape(args[2]) + "'");
					if (ch == null)
					{
						DisplayMessage(client, "Le joueur \"{0}\" est introuvable.", args[2]);
						break;
					}

					GameServer.Database.AddObject(casier = new Casier(client.Account.Name, ch.AccountName, args[3], staffOnly));
					DisplayMessage(client, "Ajouté: " + casier.Date.ToShortDateString() + " " + casier.Date.ToShortTimeString() + ": " + casier.Reason);
					break;
        	}
        }
	}
}