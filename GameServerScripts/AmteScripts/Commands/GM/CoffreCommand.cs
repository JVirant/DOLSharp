using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Commands;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		 "&coffre",
		 ePrivLevel.GM,
		 "Gestions des coffres",
		 "'/coffre create' créé un nouveau coffre (100% chance d'apparition, 1h d'intervalle entre les items)",
		 "'/coffre model <model>' change le skin du coffre selectionné",
		 "'/coffre item <chance> <interval>' change le nombre de chance d'apparition d'un item, interval d'apparition d'un item en minutes",
		 "'/coffre add <id_nb> <chance>' ajoute ou modifie un item (id_nb) avec son taux de chance d'apparition au coffre selectionné",
		 "'/coffre remove <id_nb>' retire un item (id_nb) du coffre selectionné",
		 "'/coffre name <name>' change le nom du coffre selectionné",
		 "'/coffre movehere' déplace le coffre selectionné à votre position",
		 "'/coffre delete' supprime le coffre selectionné",
		 "'/coffre reset' remet à zero la derniere fois que le coffre a été ouvert",
		 "'/coffre info' donne toutes les informations du coffre selectionné",
		 "'/coffre copy' copie le coffre selectionné à votre position",
		 "'/coffre randomcopy' copie le coffre selectionné à votre position mais change les valeurs de plus ou moin 10%",
		 "'/coffre key <id_nb>' Id_nb de la clef necessaire à l'ouverture du coffre (\"nokey\" pour retirer la clé)",
		 "'/coffre difficult <difficulté>' difficulté pour crocheter le coffre (en %) si 0, le coffre ne peut pas être crocheté")]
	public class CoffreCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if(client.Player == null) return;
			GamePlayer player = client.Player;

			if(args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			GameCoffre coffre = player.TargetObject as GameCoffre;
			switch(args[1].ToLower())
			{
					#region create - model
				case "create":
					coffre = new GameCoffre
					         	{
					         		Name = "Coffre",
					         		Model = 1596,
					         		X = player.X,
					         		Y = player.Y,
					         		Z = player.Z,
					         		Heading = player.Heading,
					         		CurrentRegionID = player.CurrentRegionID,
					         		ItemInterval = 60,
					         		ItemChance = 100
					         	};
                    coffre.LoadedFromScript = false;
					coffre.AddToWorld();
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:" + coffre.ObjectID + ")");
					break;

				case "model":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						coffre.Model = (ushort)int.Parse(args[2]);
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le model " + coffre.Model);
					break;
					#endregion

					#region item - add - remove
				case "item":
					if(coffre == null || args.Length < 4)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						coffre.ItemChance = int.Parse(args[2]);
						coffre.ItemInterval = int.Parse(args[3]);
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a maintenant " + coffre.ItemChance + " chances de faire apparaitre un item toutes les " + coffre.ItemInterval + " minutes.");
					break;

				case "add":
					if(coffre == null || args.Length < 4)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						if(!coffre.ModifyItemList(args[2], int.Parse(args[3])))
						{
							ChatUtil.SendSystemMessage(client, "L'item \""+args[2]+"\" n'existe pas !");
							break;
						}
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" peut faire apparaitre un item \""+args[2]+"\" avec un taux de chance de "+args[3]);
					break;
				case "remove":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						if(!coffre.DeleteItemFromItemList(args[2]))
						{
							ChatUtil.SendSystemMessage(client, "L'item \""+args[2]+"\" n'existe pas !");
							break;
						}
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" ne peut plus faire apparaitre l'item \""+args[2]+"\"");
					break;
					#endregion

					#region name - movehere - delete
				case "name":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.Name = args[2];
					coffre.SaveIntoDatabase();
					break;

				case "movehere":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.X = player.X;
					coffre.Y = player.Y;
					coffre.Z = player.Z;
					coffre.Heading = player.Heading;
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été déplacé à votre position.");
					break;

				case "delete":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.Delete();
					coffre.DeleteFromDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été supprimé.");
					break;
					#endregion

					#region reset - info
				case "reset":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.LastOpen = DateTime.MinValue;
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été remit à zéro.");
					break;

				case "info":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					player.Out.SendCustomTextWindow(coffre.Name, coffre.DelveInfo());
					break;
					#endregion

					#region copy - randomcopy
				case "randomcopy":
				case "copy":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}

					GameCoffre coffre2;
					if(args[1].ToLower() == "randomcopy")
					{
						List<GameCoffre.CoffreItem> items = new List<GameCoffre.CoffreItem>(coffre.Items);
						foreach(GameCoffre.CoffreItem item in items)
						{
							if(Util.Chance(50))
								item.Chance += (int)(item.Chance * Util.RandomDouble() / 10);
							else if(item.Chance > 1)
							{
								item.Chance -= (int)(item.Chance * Util.RandomDouble() / 10);
								if(item.Chance < 1)
									item.Chance = 1;
							}
						}
						coffre2 = new GameCoffre(items);
					}
					else
						coffre2 = new GameCoffre(coffre.Items);

					coffre2.Name = coffre.Name;
					coffre2.X = player.X;
					coffre2.Y = player.Y;
					coffre2.Z = player.Z;
					coffre2.Heading = player.Heading;
					coffre2.CurrentRegion = player.CurrentRegion;
					coffre2.Model = coffre.Model;
					coffre2.ItemInterval = coffre.ItemInterval;
					coffre2.ItemChance = coffre.ItemChance;
					if (args[1].ToLower() == "randomcopy")
					{
						if (Util.Chance(50))
							coffre2.ItemInterval += (int) (coffre2.ItemInterval*Util.RandomDouble()/10);
						else if (coffre2.ItemInterval > 1)
						{
							coffre2.ItemInterval -= (int) (coffre2.ItemInterval*Util.RandomDouble()/10);
							if (coffre2.ItemInterval < 1)
								coffre2.ItemInterval = 1;
						}
						if (Util.Chance(50) && coffre2.ItemChance < 100)
						{
							coffre2.ItemChance += (int) (coffre2.ItemChance*Util.RandomDouble()/10);
							if (coffre2.ItemChance > 100)
								coffre2.ItemChance = 100;
						}
						else if (coffre2.ItemChance > 0)
						{
							coffre2.ItemChance -= (int) (coffre2.ItemChance*Util.RandomDouble()/10);
							if (coffre2.ItemChance < 0)
								coffre2.ItemChance = 0;
						}
					}
					coffre2.AddToWorld();
					coffre2.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:"+coffre2.ObjectID+")");
					break;
					#endregion

					#region key - difficult
				case "key":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					if(args[2].ToLower() == "nokey")
						coffre.KeyItem = "";
					else
					{
						if (GameServer.Database.SelectObject<ItemTemplate>("Id_nb = '" + GameServer.Database.Escape(args[2]) + "'") == null)
						{
							ChatUtil.SendSystemMessage(client, "La clef ayant l'id_nb \"" + args[2] + "\" n'existe pas.");
							break;
						}
						coffre.KeyItem = args[2];
					}
					coffre.SaveIntoDatabase();

					if(coffre.KeyItem == "")
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" n'a plus besoin de clef pour être ouvert.");
					else
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a besoin de la clef ayant l'id_nb \""+coffre.KeyItem+"\" pour être ouvert.");
					break;

				case "difficult":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						int num = int.Parse(args[2]);
						if(num >= 100)
							num = 99;
						if(num < 0)
							num = 0;
						coffre.LockDifficult = num;
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					if(coffre.LockDifficult > 0)
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a maintenant une difficulté pour être crocheter de "+coffre.LockDifficult+"%.");
					else
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" ne peut plus être crocheté.");
					break;
					#endregion
			}
		}
	}
}
