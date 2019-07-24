using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&banque",
		ePrivLevel.Player,
		"Pour r�cup�rer de l'argent � la banque.",
		"/banque <Cuivre> <Argent> <Or> <Platine>",
		"/banque cheque <Cuivre> <Argent> <Or> <Platine>")]
	public class BanqueCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			try
			{
				Banquier target = client.Player.TargetObject as Banquier;
				if (target != null)
				{
					DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(client.Player.InternalID);
					if (args[1].ToLower() == "ch�que" || args[1].ToLower() == "cheque")
					{
						if (bank == null)
						{
							client.Out.SendMessage(
								"Vous n'avez pas de compte, vous ne pouvez donc pas faire de ch�que !",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}

						long newMoney = GetMoney(args, 2);
						if (newMoney > 1000000000)
						{
							client.Out.SendMessage("Vous pouvez faire un ch�que de maximum 100 platines !", eChatType.CT_System,
								eChatLoc.CL_PopupWindow);
							return;
						}
						if (!Banquier.TakeMoney(bank, client.Player, newMoney))
						{
							client.Out.SendMessage("Vous n'avez pas assez d'argent � la banque !", eChatType.CT_System,
								eChatLoc.CL_SystemWindow);
							return;
						}


						ItemUnique item = new ItemUnique
							{
								Model = 499,
								Id_nb = "BANQUE_CHEQUE_" + client.Player.Name + "_" + Environment.TickCount.ToString("X8"),
								Price = newMoney,
								Weight = 2,
								Name = "Ch�que de " + client.Player.Name,
								Description =
									"Ce ch�que peut �tre �chang� contre la somme de " + Money.GetString(newMoney) +
										" au banquier.\n\nAttention: la vente du ch�que ne vous donnerait que la moiti� de sa valeur !"
							};
						GameServer.Database.AddObject(item);

						string message = "";
						if (!client.Player.Inventory.AddTemplate(GameInventoryItem.Create(item), 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
						{
							if (!client.Player.Inventory.AddTemplate(GameInventoryItem.Create(item), 1, eInventorySlot.FirstVault, eInventorySlot.LastVault))
							{
								bank.Money += newMoney;
								GameServer.Database.SaveObject(bank);
								GameServer.Database.DeleteObject(item);
								client.Out.SendMessage("Vous n'avez pas assez de place dans votre sac et dans votre coffre pour le ch�que !",
									eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							message += "Vous n'avez plus de place dans votre sac, j'ai donn� le ch�que au gardien des coffres.\n";
						}
						message += "Vous avez maintenant " + Money.GetString(bank.Money) + " dans votre compte.";
						client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        InventoryLogging.LogInventoryAction(client.Player, target, eInventoryActionType.Other, newMoney);
                        InventoryLogging.LogInventoryAction(target, client.Player, eInventoryActionType.Other, item);
					}
					else
					{
						if (bank == null)
						{
							client.Out.SendMessage("Vous n'avez pas de compte, vous ne pouvez donc pas r�cup�rer d'argent !",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}

						long newMoney = GetMoney(args, 1);
						if (!Banquier.WithdrawMoney(bank, client.Player, newMoney))
						{
							client.Out.SendMessage("Vous n'avez pas assez d'argent � la banque !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}

						string message = "Vous avez maintenant " + Money.GetString(bank.Money) + " dans votre compte.";
						client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        InventoryLogging.LogInventoryAction(target, client.Player, eInventoryActionType.Other, newMoney);
					}
				}
				else
				{
					client.Out.SendMessage("Vous devez s�lectionner un banquier pour r�cup�rer de l'argent !", eChatType.CT_System,
						eChatLoc.CL_SystemWindow);
				}
				client.Out.SendUpdateMoney();
			}
			catch (Exception)
			{
				DisplaySyntax(client);
			}
		}

		private static long GetMoney(string[] args, int offset)
		{
			int C;
			int S = 0;
			int G = 0;
			int P = 0;
			if (int.TryParse(args[offset], out C))
			{
				if (int.TryParse(args[offset + 1], out S))
				{
					if (int.TryParse(args[offset + 2], out G))
					{
						if (int.TryParse(args[offset + 3], out P))
							P = Math.Max(0, Math.Min(P, 999));
						G = Math.Max(0, Math.Min(G, 999));
					}
					S = Math.Max(0, Math.Min(S, 99));
				}
				C = Math.Max(0, Math.Min(C, 99));
			}

			return Money.GetMoney(0, P, G, S, C);
		}
	}
}