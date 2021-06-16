using System;
using System.Collections.Generic;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using System.Reflection;
using log4net;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		 "&textnpc",
		 ePrivLevel.GM,
		 "Gestions des TextNPC",
		 "'/textnpc create' créé un nouveau pnj",
         "'/textnpc createmerchant' créé un nouveau marchand qui parle",
         "'/textnpc createguard' créé un garde qui parle",
		 "'/textnpc reponse' affiche les réponses du pnj (les 20 premières lettres de la réponse)",

		 //text
		 "'/textnpc text <texte>' définit le texte d'intéraction (clic droit) (mettez le caractère | ou ; pour les sauts de ligne)",
		 "'/textnpc add <reponse> <texte>' ajoute ou modifie la réponse 'reponse' (mettez le caractère | ou ; pour les sauts de ligne)",
		 "'/textnpc remove <reponse>' retire la réponse 'reponse'",

		 //emote
		 "'/textnpc emote add <emote> <reponse>'",
		 "'/textnpc emote remove <reponse>'",
		 "'/textnpc emote help'",

		 //Spell
		 "'/textnpc spell add <spellID> <reponse>'",
		 "'/textnpc spell remove <reponse>'",
		 "'/textnpc spell help'",

		 //phrase cc general
		 "'/textnpc randomphrase add <emote (0=aucune)> <say/yell/em> <phrase>'",
		 "'/textnpc randomphrase remove <phrase>'",
		 "'/textnpc randomphrase interval <interval en secondes>'",
		 "'/textnpc randomphrase help'",
		 "'/textnpc randomphrase view'",

		 //conditions
         "'/textnpc quest <None/Availabe/Lesson/Lore/Finish/Pending>' affiche ou non l'icone pour la quête",
		 "'/textnpc level <levelmin> <levelmax>' règle le niveau minimum et maximum des personnage pouvant parler au pnj",
		 "'/textnpc guild add <guildname>' ajoute une guilde à laquelle le pnj ne parle pas (mettre 'NO GUILD' pour les non guildé)",
		 "'/textnpc guild remove <guildname>' retire une guilde à laquelle le pnj ne parle pas",
         "'/textnpc guildA add <guildname>' ajoute une guilde à laquelle le pnj parle (mettre 'NO GUILD' pour les non guildé et 'ALL' pour toutes les guildes)",
         "'/textnpc guildA remove <guildname>' retire une guilde à laquelle le pnj parle",
		 "'/textnpc race add <race name>' ajoute une race à laquelle le pnj ne parle pas",
		 "'/textnpc race remove <race name>' retire une race à laquelle le pnj ne parle pas",
		 "'/textnpc race list' liste les races disponible",
		 "'/textnpc class add <class name>' ajoute une classe à laquelle le pnj ne parle pas",
		 "'/textnpc class remove <class name>' retire une classe à laquelle le pnj ne parle pas",
		 "'/textnpc class list' liste les classes disponible",
		 "'/textnpc hour <hour min> <hour max>' règle l'heure à laquelle le pnj parle (fonctionne aussi pr les phrases aléatoire)",
		 "'/textnpc reput <reput min> <reput max>' règle la réputation du joueur à laquelle le pnj parle",
		 "'/textnpc condition list' liste les conditions du pnj",
		 "'/textnpc condition help' donne plus d'information sur les conditions",


		 "Dans chaque texte: {0} = nom du joueur, {1} = nom de famille, {2} = guilde, {3} = classe, {4} = race")]
	public class TextNPCCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void OnCommand(GameClient client, string[] args)
		{
			if(client.Player == null) return;
			GamePlayer player = client.Player;

			if(args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			ITextNPC npc = player.TargetObject as ITextNPC;
			string text = "";
			string reponse = "";
			IList<string> lines;
			switch(args[1].ToLower())
			{
					#region create - view - reponse - text
				case "create":
                case "createmerchant":
                case "createguard":
                    if (args[1].ToLower() == "create") npc = new TextNPC();
                    else if (args[1].ToLower() == "createmerchant") npc = new TextNPCMerchant();
                    else if (args[1].ToLower() == "createguard") npc = new GuardTextNPC();

                    if(npc == null) npc = new TextNPC();
                    ((GameNPC)npc).LoadedFromScript = false;
					((GameNPC)npc).Position = player.Position;
                    ((GameNPC)npc).Heading = player.Heading;
                    ((GameNPC)npc).CurrentRegion = player.CurrentRegion;
                    ((GameNPC)npc).Name = "Nouveau pnj";
                    ((GameNPC)npc).Realm = 0;
					if ((((GameNPC)npc).Flags & GameNPC.eFlags.PEACE) == 0)
						((GameNPC) npc).Flags ^= GameNPC.eFlags.PEACE;
                    ((GameNPC)npc).Model = 40;
					npc.TextNPCData.Interact_Text = "Texte à définir.";
                    ((GameNPC)npc).AddToWorld();
					((GameNPC)npc).SaveIntoDatabase();
					break;

				case "reponse":
					if(npc == null)
					{
						DisplaySyntax(client);
						return;
					}
                    if (npc.TextNPCData.Reponses != null && npc.TextNPCData.Reponses.Count > 0)
					{
                        foreach (var de in npc.TextNPCData.Reponses)
						{
							if(text.Length > 1)
								text += "\n";
							if(de.Value.Length > 20)
								text += "[" + de.Key + "] Réponse: " + de.Value.Substring(0, 20).Trim('[', ']') + "...";
							else
								text += "[" + de.Key + "] Réponse: " + de.Value.Trim('[', ']') + "...";
						}
					}
					else
						text = "Ce pnj n'a aucune réponse de défini.";
					player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;

				case "text":
					if(npc == null || args.Length < 3)
					{
						DisplaySyntax(client);
						return;
					}
					text = string.Join(" ", args, 2, args.Length - 2);
					text = text.Replace('|', '\n');
					text = text.Replace(';', '\n');
                    if (text == "NO TEXT")
                        npc.TextNPCData.Interact_Text = "";
                    else
                        npc.TextNPCData.Interact_Text = text;
                    npc.TextNPCData.SaveIntoDatabase();
					player.Out.SendMessage("Texte défini:\n" + text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
					#endregion

					#region add - remove
				case "add":
					if(npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					reponse = args[2];
					string texte = string.Join(" ", args, 3, args.Length-3);
					texte = texte.Replace('|', '\n');
					texte = texte.Replace(';', '\n');
                    if (npc.TextNPCData.Reponses.ContainsKey(reponse))
					{
                        npc.TextNPCData.Reponses[reponse] = texte;
						player.Out.SendMessage("Réponse \""+reponse+"\" modifié avec le texte:\n"+texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					}
                    else
					{
                        npc.TextNPCData.Reponses.Add(reponse, texte);
						player.Out.SendMessage("Réponse \""+reponse+"\" ajouté avec le texte:\n"+texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					}
                    npc.TextNPCData.SaveIntoDatabase();
					break;

				case "remove":
					if(npc == null || args.Length < 3)
					{
						DisplaySyntax(client);
						return;
					}
					if(npc.TextNPCData.Reponses != null && npc.TextNPCData.Reponses.Count > 0)
					{
						if(npc.TextNPCData.Reponses.ContainsKey(args[2]))
						{
							text = "La réponse \""+args[2]+"\" a été supprimé dont le texte était:\n"+npc.TextNPCData.Reponses[args[2]];
							npc.TextNPCData.Reponses.Remove(args[2]);
							npc.TextNPCData.SaveIntoDatabase();
						}
						else
							text = "Ce pnj n'a pas de réponse \""+args[2]+"\" défini.";
					}
					else
						text = "Ce pnj n'a aucune réponse de défini.";
					player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
					#endregion

					#region emote add/remove/help
				case "emote":
					if(npc == null || args.Length < 2)
					{
						DisplaySyntax(client);
						return;
					}
					if(args.Length > 4)
						reponse = string.Join(" ", args, 4, args.Length - 4);
					if(args[2].ToLower() == "add")
					{
						if(args.Length < 5)
						{
							DisplaySyntax(client);
							return;
						}
						try
						{
                            if (npc.TextNPCData.EmoteReponses.ContainsKey(reponse))
							{
                                npc.TextNPCData.EmoteReponses[reponse] = (eEmote)Enum.Parse(typeof(eEmote), args[3], true);
								player.Out.SendMessage("Emote réponse \""+reponse+"\" modifié", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else
							{
                                npc.TextNPCData.EmoteReponses.Add(reponse, (eEmote)Enum.Parse(typeof(eEmote), args[3], true));
								player.Out.SendMessage("Emote réponse \""+reponse+"\" ajouté", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							npc.TextNPCData.SaveIntoDatabase();
						}
						catch
						{
							player.Out.SendMessage("L'emote n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
					}
					else if(args[2].ToLower() == "remove")
					{
						if(args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
                        if (npc.TextNPCData.EmoteReponses.ContainsKey(reponse))
						{
                            npc.TextNPCData.EmoteReponses.Remove(reponse);
							npc.TextNPCData.SaveIntoDatabase();
							player.Out.SendMessage("Emote réponse \""+reponse+"\" supprimée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
					}
					else if(args[2].ToLower() == "help")
					{
						lines = new List<string>();
						lines.Add("Si la réponse est 'INTERACT' (sans les guillemets) alors l'emote sera faite lorsque le joueur parle au pnj (clic droit)");
						lines.Add("Liste des emotes:");
						foreach(string t in Enum.GetNames(typeof(eEmote)))
							lines.Add(t);
						player.Out.SendCustomTextWindow("Les emote réponses pour les nuls !", lines);
					}
					break;
					#endregion

					#region spell add/remove/help
				case "spell":
					if(npc == null || args.Length < 3)
					{
						DisplaySyntax(client);
						return;
					}
                    if(args.Length > 4)
					    reponse = string.Join(" ", args, 4, args.Length - 4);
					if(args[2].ToLower() == "add")
					{
						if(args.Length < 5)
						{
							DisplaySyntax(client);
							return;
						}
						try
						{
                            if (npc.TextNPCData.SpellReponses.ContainsKey(reponse))
							{
                                npc.TextNPCData.SpellReponses[reponse] = ushort.Parse(args[3]);
								player.Out.SendMessage("Spell réponse \""+reponse+"\" modifiée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else
							{
                                npc.TextNPCData.SpellReponses.Add(reponse, ushort.Parse(args[3]));
								player.Out.SendMessage("Spell réponse \""+reponse+"\" ajoutée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							npc.TextNPCData.SaveIntoDatabase();
						}
						catch(Exception e)
						{
							log.Debug("ERROR: ", e);
							player.Out.SendMessage("Le spellid n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
					}
					else if(args[2].ToLower() == "remove")
					{
						if(args.Length < 4)
						{
							DisplaySyntax(client);
							return;
						}
                        if (npc.TextNPCData.SpellReponses.ContainsKey(reponse))
						{
                            npc.TextNPCData.SpellReponses.Remove(reponse);
							npc.TextNPCData.SaveIntoDatabase();
							player.Out.SendMessage("Spell réponse \""+reponse+"\" supprimée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
						else
							player.Out.SendMessage("Ce pnj n'a pas de spell réponse '"+reponse+"'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "help")
					{
						lines = new List<string>();
						lines.Add("Si la réponse est 'INTERACT' (sans les guillemets) alors l'animation du spell sera faite lorsque le joueur parle au pnj (clic droit).");
						player.Out.SendCustomTextWindow("Les spell réponses pour les nuls !", lines);
					}
					break;
					#endregion

					#region randomphrase add/remove/interval/help/view
				case "randomphrase":
					if(npc == null || args.Length < 3)
					{
						DisplaySyntax(client);
						return;
					}
					if(args[2].ToLower() == "add")
					{
						if(args.Length < 6 || (args[4].ToLower() != "say" && args[4].ToLower() != "yell" && args[4].ToLower() != "em"))
						{
							DisplaySyntax(client);
							return;
						}
						reponse = args[4].ToLower() + ":" + string.Join(" ", args, 5, args.Length - 5);
						try
						{
                            if (npc.TextNPCData.RandomPhrases.ContainsKey(reponse))
							{
								if (args[3] != "0")
                                    npc.TextNPCData.RandomPhrases[reponse] = (eEmote)Enum.Parse(typeof(eEmote), args[3], true);
								else
                                    npc.TextNPCData.RandomPhrases[reponse] = 0;
								player.Out.SendMessage("L'emote de la phrase \""+reponse+"\" a été modifié", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else
							{
                                npc.TextNPCData.RandomPhrases.Add(reponse, (eEmote)Enum.Parse(typeof(eEmote), args[3], true));
								player.Out.SendMessage("La phrase \""+reponse+"\" a été ajouté", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							npc.TextNPCData.SaveIntoDatabase();
						}
						catch
						{
							player.Out.SendMessage("L'emote n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
					}

					if(args[2].ToLower() == "remove")
					{
						if(args.Length < 4)
						{
							DisplaySyntax(client);
							return;
						}
						text = string.Join(" ", args, 3, args.Length - 3);
                        if (npc.TextNPCData.RandomPhrases.ContainsKey("say:" + text) ||
                            npc.TextNPCData.RandomPhrases.ContainsKey("yell:" + text) ||
                            npc.TextNPCData.RandomPhrases.ContainsKey("em:" + text))
						{
                            if (npc.TextNPCData.RandomPhrases.ContainsKey("say:" + text))
                                npc.TextNPCData.RandomPhrases.Remove("say:" + text);
                            else if (npc.TextNPCData.RandomPhrases.ContainsKey("em:" + text))
                                npc.TextNPCData.RandomPhrases.Remove("yell:" + text);
							else
                                npc.TextNPCData.RandomPhrases.Remove("em:" + text);
							npc.TextNPCData.SaveIntoDatabase();
							player.Out.SendMessage("Phrase \""+text+"\" supprimée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
						else
							player.Out.SendMessage("Ce pnj n'a pas de phrase '"+text+"'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}

					if(args[2].ToLower() == "interval")
					{
						if(args.Length < 4)
						{
							DisplaySyntax(client);
							return;
						}
						try
						{
                            npc.TextNPCData.PhraseInterval = int.Parse(args[3]);
							npc.TextNPCData.SaveIntoDatabase();
						}
						catch
						{
							player.Out.SendMessage("L'interval n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
					}

					if(args[2].ToLower() == "view")
					{
                        if (npc.TextNPCData.RandomPhrases.Count < 1)
						{
							player.Out.SendMessage("Ce pnj n'a pas de phrase.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						lines = new List<string>();
                        lines.Add("Phrases que peut dire le pnj à un interval de " + npc.TextNPCData.PhraseInterval + " secondes:");
                        foreach (var de in npc.TextNPCData.RandomPhrases)
							lines.Add(de.Key + " - Emote: " + de.Value);
						player.Out.SendCustomTextWindow("Les phrases de " + ((GameNPC) npc).Name, lines);
					}

					if(args[2].ToLower() == "help")
					{
						lines = new List<string>();
						lines.Add("Pour ajouter une phrase, utilisez '/textnpc randomphrase <emote> <say/yell> <phrase>'.");
						lines.Add("emote: Si l'emote est '0' alors il n'y aura pas d'emote lorsque le pnj dira la phrase. (voir '/textnpc emote help' pour les emotes possibles').");
						lines.Add("say/yell/em: C'est le type de phrase envoyé par le pnj, si c'est 'say' le pnj parlera sur le cc général, si c'est 'yell' le pnj parlera fort (rayon d'entente plus grand) sur le cc général, 'em' est utilisé pour les actions comme '/em <text>'.");
						lines.Add("phrase: La phrase est choisite aléatoirement dans toutes les phrases disponibles.");
						player.Out.SendCustomTextWindow("Les phrases aléatoire pour les nuls !", lines);
					}
					break;
					#endregion

					#region level/guild/race/class/prp/hour/karma
                case "quest":
	                eQuestIndicator indicator;
                    if (npc == null || args.Length < 3 || !Enum.TryParse(args[2], out indicator))
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    npc.TextNPCData.Condition.CanGiveQuest = indicator;
                    npc.TextNPCData.SaveIntoDatabase();
                    break;

				case "level":
					if(npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					try
					{
						int min = int.Parse(args[2]);
						int max = int.Parse(args[3]);
						if(min < 1) 
							min = 1;
						else if(min > 50)
							max = 50;
						if(max < min)
							max = min;
						if(max > 50)
							max = 50;
                        npc.TextNPCData.Condition.Level_min = min;
                        npc.TextNPCData.Condition.Level_max = max;
						npc.TextNPCData.SaveIntoDatabase();
					}
					catch
					{
						player.Out.SendMessage("Le level max ou min n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					player.Out.SendMessage(
                        "Le niveau est maintenant de " + npc.TextNPCData.Condition.Level_min + " minimum et " + npc.TextNPCData.Condition.Level_max + " maximum.",
						eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				case "guild":
					if(npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					if(args[2].ToLower() == "add")
					{
						if(!GuildMgr.DoesGuildExist(args[3]) && args[3] != "NO GUILD")
						{
							player.Out.SendMessage("La guilde \""+ args[3] +"\" n'éxiste pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.GuildNames.Add(args[3]);
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La guilde "+args[3]+" a été ajouté aux guildes interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "remove")
					{
                        if (npc.TextNPCData.Condition.GuildNames.Count < 1 || !npc.TextNPCData.Condition.GuildNames.Contains(args[3]))
						{
							player.Out.SendMessage("Ce pnj n'a pas d'interdiction sur la guilde \""+ args [3] +"\".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.GuildNames.Remove(args[3]);
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La guilde "+args[3]+" a été retirée des guildes interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
						DisplaySyntax(client);
					break;

                case "guilda":
                    if (npc == null || args.Length < 4)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (args[2].ToLower() == "add")
                    {
                        if (!GuildMgr.DoesGuildExist(args[3]) && (args[3] != "NO GUILD" || args[3] != "ALL"))
                        {
                            player.Out.SendMessage("La guilde \"" + args[3] + "\" n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            break;
                        }
                        if (npc.TextNPCData.Condition.GuildNamesA.Contains(args[3]))
						{
							DisplayMessage(client, "La guilde \"{0}\" a déjà été ajouté.", args[3]);
							return;
						}
                        npc.TextNPCData.Condition.GuildNamesA.Add(args[3]);
                        npc.TextNPCData.SaveIntoDatabase();
                        player.Out.SendMessage("La guilde " + args[3] + " a été ajouté aux guildes autorisées.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (args[2].ToLower() == "remove")
                    {
                        if (npc.TextNPCData.Condition.GuildNamesA.Count < 1 || !npc.TextNPCData.Condition.GuildNamesA.Contains(args[3]))
                        {
                            player.Out.SendMessage("Ce pnj n'a pas d'autorisation sur la guilde \"" + args[3] + "\".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            break;
                        }
                        npc.TextNPCData.Condition.GuildNamesA.Remove(args[3]);
                        npc.TextNPCData.SaveIntoDatabase();
                        player.Out.SendMessage("La guilde " + args[3] + " a été retirée des guildes autorisées.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else
                        DisplaySyntax(client);
                    break;

				case "race":
					if(npc == null || args.Length < 3 || (args.Length < 4 && args[2].ToLower() != "list"))
					{
						DisplaySyntax(client);
						return;
					}
					if(args[2].ToLower() == "add")
					{
						if(!_RaceNameExist(args[3]))
						{
							player.Out.SendMessage("La race \""+ args[3] +"\" n'éxiste pas, voir '/textnpc race list'", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.Races.Add(args[3].ToLower());
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La race "+args[3]+" a été ajouté aux races interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "remove")
					{
                        if (npc.TextNPCData.Condition.Races.Count < 1 || !npc.TextNPCData.Condition.Races.Contains(args[3]))
						{
							player.Out.SendMessage("Ce pnj n'a pas d'interdiction sur la race \""+ args [3] +"\".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.Races.Remove(args[3].ToLower());
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La race "+args[3]+" a été retirée des races interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "list")
					{
						lines = new List<string>();
						lines.Add("Liste des races existantes:");
						//TODO: races
						//foreach(string race in GamePlayer.RACENAMES)
							//lines.Add(race);
						player.Out.SendCustomTextWindow("Les races pour les nuls !", lines);
					}
					else
						DisplaySyntax(client);
					break;

				case "class":
					if(npc == null || args.Length < 3 || (args.Length < 4 && args[2].ToLower() != "list"))
					{
						DisplaySyntax(client);
						return;
					}
					if(args[2].ToLower() == "add")
					{
						if(!_ClassNameExist(args[3]))
						{
							player.Out.SendMessage("La classe \""+ args[3] +"\" n'éxiste pas, voir '/textnpc class list'", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.Classes.Add(args[3].ToLower());
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La classe "+args[3]+" a été ajouté aux classes interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "remove")
					{
                        if (npc.TextNPCData.Condition.Classes.Count < 1 || !npc.TextNPCData.Condition.Classes.Contains(args[3]))
						{
							player.Out.SendMessage("Ce pnj n'a pas d'interdiction sur la classe \""+ args [3] +"\".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
                        npc.TextNPCData.Condition.Classes.Remove(args[3].ToLower());
						npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage("La classe "+args[3]+" a été retirée des classes interdites.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else if(args[2].ToLower() == "list")
					{
						lines = new List<string>();
						lines.Add("Liste des classes existantes:");
						foreach(string classe in Enum.GetNames(typeof(eCharacterClass)))
							lines.Add(classe);
						player.Out.SendCustomTextWindow("Les classes pour les nuls !", lines);
					}
					else
						DisplaySyntax(client);
					break;

				case "hour":
					if(npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					try
					{
						int min = int.Parse(args[2]);
						int max = int.Parse(args[3]);
                        npc.TextNPCData.Condition.Heure_min = min;
                        npc.TextNPCData.Condition.Heure_max = max;
						npc.TextNPCData.SaveIntoDatabase();
					}
					catch
					{
						player.Out.SendMessage("L'heure max ou min n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					player.Out.SendMessage("L'heure est maintenant comprise entre "+npc.TextNPCData.Condition.Heure_min+"h et "+npc.TextNPCData.Condition.Heure_max+"h.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				case "reputation":
				case "reput":
					if (npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					try
					{
						var min = float.Parse(args[2].Replace('.', ','));
						var max = float.Parse(args[3].Replace('.', ','));
						npc.TextNPCData.Condition.Reput_min = min;
						npc.TextNPCData.Condition.Reput_max = max;
						npc.TextNPCData.SaveIntoDatabase();
					}
					catch
					{
						player.Out.SendMessage("La réputation max ou min n'est pas valide.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					player.Out.SendMessage("La réputation est maintenant comprise entre " + npc.TextNPCData.Condition.Reput_min + " et " + npc.TextNPCData.Condition.Reput_max + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;
					#endregion

					#region condition list/help
				case "condition":
					if(args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					if(args[2].ToLower() == "list" && npc != null)
					{
						lines = new List<string>
						        {
						        	"Conditions du pnj " + ((GameNPC) npc).Name + ":",
						        	"+ Heure      min: " + npc.TextNPCData.Condition.Heure_min + " max:" +
						        	npc.TextNPCData.Condition.Heure_max,
						        	"+ Réputation min: " + npc.TextNPCData.Condition.Reput_min + " max:" +
						        	npc.TextNPCData.Condition.Reput_max
						        };
						if (npc.TextNPCData.Condition.Level_min != 1 || npc.TextNPCData.Condition.Level_max != 50)
                            lines.Add("+ Level      min: " + npc.TextNPCData.Condition.Level_min + " max: " + npc.TextNPCData.Condition.Level_max);
						if(npc.TextNPCData.Condition.GuildNames != null && npc.TextNPCData.Condition.GuildNames.Count > 0)
						{
							lines.Add("+ Guildes interdites:");
							foreach(string guild in npc.TextNPCData.Condition.GuildNames)
								lines.Add("   " + guild);
						}
						if (npc.TextNPCData.Condition.GuildNames != null && npc.TextNPCData.Condition.GuildNames.Count > 0)
						{
							lines.Add("+ Guildes autorisées:");
							foreach (string guild in npc.TextNPCData.Condition.GuildNamesA)
								lines.Add("   " + guild);
						}
						if(npc.TextNPCData.Condition.Races != null && npc.TextNPCData.Condition.Races.Count > 0)
						{
							lines.Add("+ Races interdites:");
							foreach(string race in npc.TextNPCData.Condition.Races)
								lines.Add("   " + race);
						}
						if(npc.TextNPCData.Condition.Classes != null && npc.TextNPCData.Condition.Classes.Count > 0)
						{
							lines.Add("+ Classes interdites:");
							foreach(string classe in npc.TextNPCData.Condition.Classes)
								lines.Add("   " + classe);
						}
                        if (npc.TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None)
                            lines.Add($"+ Quêtes: {npc.TextNPCData.Condition.CanGiveQuest}");
                        player.Out.SendCustomTextWindow("Conditions de " + ((GameNPC)npc).Name, lines);
					}
					else if(args[2].ToLower() == "help")
					{
						lines = new List<string>
						        {
						        	"Type de conditions:",
						        	"+ Level: on règle le niveau minimum et maximum des personnages auquels le pnj parlera. Par exemple, si l'on met 15 en minimum et 50 en maximum, le pnj parlera aux personnages du niveau 15 au niveau 49.",
						        	"+ Guilde: on ajoute les guildes auquelles le pnj ne parlera pas, donc si l'on met par exemple la guilde 'Legion Noire', le pnj ne parlera pas aux membre de la Legion Noire. Pour que le pnj ne parle pas au non guildé, il faut ajouter la guilde 'NO GUILD'.",
						        	"+ Race/Classe: on ajoute les races ou classes auquelles le pnj ne parlera pas, donc si on ajoute 'Troll', le pnj ne parlera pas aux trolls. (Voir '/textnpc race list' et '/textnpc class list' pour voir les races/classes possible).",
						        	"+ Les heures: on règle la tranche d'heure du jeu pendant laquelle parle le pnj. Pour mettre une tranche d'heure de nuit par exemple de 22h à 5h. Il faut mettre 22 en minimum et 5 en maximum, le pnj parlera de 22h00 à 4h59 (heure du jeu). (Cette condition fonctionne aussi pour les phrases/emotes aléatoires",
						        	"+ Quêtes: permet juste d'afficher ou non l'icone de quêtes."
						        };
						player.Out.SendCustomTextWindow("Les conditions pour les nuls !", lines);
					}
					else
						DisplaySyntax(client);
					break;
					#endregion

				default:
					DisplaySyntax(client);
					break;
			}
			return;
		}

		//TODO: need a new Array
		/*
		private bool RaceNameExist(string name)
		{
			foreach(string race in GamePlayer.RACENAMES())
				if(race.ToLower() == name.ToLower())
					return true;
			return false;
		}
		*/
		private static bool _RaceNameExist(string name)
		{
			return true;
		}

		private static bool _ClassNameExist(string name)
		{
			foreach (string classe in Enum.GetNames(typeof(eCharacterClass)))
				if (classe.ToLower() == name.ToLower())
					return true;
			return false;
		}
	}
}
