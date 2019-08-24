using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&areaeffect",
		ePrivLevel.GM,
		"Gestion d'AreaEffect",
		"'/areaeffect create <radius> <ID de l'effet>'",
		"'/areaeffect <heal/harm> <valeur>'",
        "'/areaeffect <mana> <valeur>'",
        "'/areaeffect <endurance> <valeur>'",
		"'/areaeffect radius <newRadius>'",
		"'/areaeffect effect <newEffect>'",
		"'/areaeffect interval <min> [max]'",
		"'/areaeffect missChance <chance %>'",
		"'/areaeffect message <message>' {0} = les points de vie ajouté/retiré, {1} = mana ajouté/retiré, {2} = endu ajouté/retiré.",
		"'/areaeffect info' Donne les informations sur l'areaeffect sélectionné")]
	public class AreaEffectCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			var AE = client.Player.TargetObject as AreaEffect;

			if (args.Length == 2 && args[1].ToLower() == "info" && AE != null)
			{
				var infos = new List<string>
				    {
				        "- Information de l'AreaEffect " + AE.Name,
				        "Vie: " + AE.HealHarm + " points de vie (+/- 10%).",
                        "Mana: " + AE.AddMana + " points de mana.",
                        "Endurance: " + AE.AddEndurance + " points d'endurance.",
				        "Rayon: " + AE.Radius,
				        "Spell: " + AE.SpellEffect,
				        "Interval entre chaque effet " + AE.IntervalMin + " à " + AE.IntervalMax + " secondes",
				        "Chance de miss: " + AE.MissChance + "%",
				        "Message: " + AE.Message
				    };
			    client.Out.SendCustomTextWindow("Info AreaEffect", infos);
				return;
			}

			if (args.Length < 3 || (AE == null && args[1].ToLower() != "create"))
			{
				DisplaySyntax(client);
				return;
			}

			var player = client.Player;
			switch (args[1].ToLower())
			{
				case "create":
					if (args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					int Radius;
					int EffectID;
					if (!int.TryParse(args[2], out Radius) || !int.TryParse(args[3], out EffectID))
					{
						DisplaySyntax(client);
						return;
					}
					AE = new AreaEffect
					    {
					        X = player.X,
					        Y = player.Y,
					        Z = player.Z,
					        CurrentRegion = player.CurrentRegion,
					        Heading = player.Heading,
					        Model = 1,
					        Name = "Area Effect",
					        Radius = Radius,
					        SpellEffect = EffectID,
                            Flags = GameNPC.eFlags.DONTSHOWNAME | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.PEACE | GameNPC.eFlags.FLYING,
					        LoadedFromScript = false
					    };
			        AE.AddToWorld();
					client.Out.SendMessage("Création d'un AreaEffect, OID:" + AE.ObjectID, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

                case "heal":
                case "harm":
			        {
			            int value;
			            if (!int.TryParse(args[2], out value))
			            {
			                DisplaySyntax(client);
			                return;
			            }
			            if (args[1].ToLower() == "harm" && value > 0)
			                value = -value;
			            AE.HealHarm = value;
			            client.Out.SendMessage(
			                AE.Name + (AE.HealHarm > 0 ? " heal" : " harm") + " maintenant de " + Math.Abs(AE.HealHarm) + " points de vie.",
			                eChatType.CT_System, eChatLoc.CL_SystemWindow);
			            break;
			        }

                case "mana":
			        {
			            int value;
			            if (!int.TryParse(args[2], out value))
			            {
			                DisplaySyntax(client);
			                return;
			            }
			            AE.AddMana = value;
			            client.Out.SendMessage(
                            AE.Name + " ajoute maintenant " + AE.AddMana + " points de mana.",
			                eChatType.CT_System, eChatLoc.CL_SystemWindow);
			            break;
			        }

                case "endu":
                case "endurance":
                    {
                        int value;
                        if (!int.TryParse(args[2], out value))
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        AE.AddEndurance = value;
                        client.Out.SendMessage(
                            AE.Name + " ajoute maintenant " + AE.AddEndurance + " points de mana.",
                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        break;
                    }

			    case "radius":
					int radius;
					if (!int.TryParse(args[2], out radius))
					{
						DisplaySyntax(client);
						return;
					}
					AE.Radius = radius;
					client.Out.SendMessage(AE.Name + " a maintenant un rayon d'effet de " + AE.Radius, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "effect":
					int effect;
					if (!int.TryParse(args[2], out effect))
					{
						DisplaySyntax(client);
						return;
					}
					AE.SpellEffect = effect;
					client.Out.SendMessage(AE.Name + " a maintenant l'effet du spell " + AE.SpellEffect, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "interval":
					int min;
					if (!int.TryParse(args[2], out min))
					{
						DisplaySyntax(client);
						return;
					}
					int max = 0;
					if (args.Length >= 4 && !int.TryParse(args[3], out max))
					{
						DisplaySyntax(client);
						return;
					}
					max = Math.Max(min, max);
					if (min > max)
					{
						int i = max;
						max = min;
						min = i;
					}
					AE.IntervalMin = min;
					AE.IntervalMax = max;
					client.Out.SendMessage(AE.Name + " a maintenant un interval compris entre " + min + " et " + max,
					                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				case "miss":
				case "chance":
				case "misschance":
					int chance;
					if (!int.TryParse(args[2], out chance))
					{
						DisplaySyntax(client);
						return;
					}
					if (chance > 100)
						chance = 100;
					else if (chance < 0)
						chance = 0;
					AE.MissChance = chance;
					client.Out.SendMessage(AE.Name + " a maintenant " + chance + " chance de raté.", eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "message":
					AE.Message = string.Join(" ", args, 2, args.Length - 2);
					client.Out.SendMessage(
						AE.Name + " a maintenant le message \"" + AE.Message + "\" lorsqu'on est touché par son effect.",
						eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				default:
					DisplaySyntax(client);
					break;
			}
			AE.SaveIntoDatabase();
		}
	}
}