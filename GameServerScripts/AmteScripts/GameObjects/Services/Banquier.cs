using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
	/// <summary>
	/// Summary description for Banquier.
	/// </summary>
	public class Banquier : GameNPC
	{
		public Banquier()
		{
            SetOwnBrain(new BlankBrain());
			GuildName = "Banquier";
		}

		public override bool ReceiveMoney(GameLiving source, long money)
		{
			return ReceiveMoney(source, money, true);
		}

		public bool ReceiveMoney(GameLiving source, long money, bool removeMoney)
		{
			if(source==null || money<=0) return false;
			if(!(source is GamePlayer))
				return false;
			GamePlayer player = source as GamePlayer;
            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
			{
				player.Out.SendMessage("Vous venez de cr�er un compte � la banque.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				bank = new DBBanque(player.InternalID);
				GameServer.Database.AddObject(bank);
			}

			if (removeMoney)
			{
				if (!player.RemoveMoney(money))
				{
					player.Out.SendMessage("Vous n'avez pas cette somme sur vous !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					return false;
				}
                InventoryLogging.LogInventoryAction(source, this, eInventoryActionType.Other, money);
			}
			bank.Money = money+bank.Money;
			
			GameServer.Database.SaveObject(bank);

			string message = "";
			if(Money.GetMithril(bank.Money) != 0)
				message += Money.GetMithril(bank.Money)+"M ";
			if(Money.GetPlatinum(bank.Money) != 0)
				message += Money.GetPlatinum(bank.Money)+"P ";
			if(Money.GetGold(bank.Money) != 0)
				message += Money.GetGold(bank.Money)+"O ";
			if(Money.GetSilver(bank.Money) != 0)
				message += Money.GetSilver(bank.Money)+"A ";
			if(Money.GetCopper(bank.Money) != 0)
				message += Money.GetCopper(bank.Money)+"C ";

			if(message != "")
				message += "Vous avez "+message+"dans votre compte.";
			else 
				message = "Vous n'avez pas d'argent dans votre compte";
			player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return true;
		}

		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			if (!(source is GamePlayer)) return false;
			GamePlayer player = source as GamePlayer;
			if (!item.Id_nb.StartsWith("BANQUE_CHEQUE")) return false;

            if (player.Inventory.RemoveCountFromStack(item, item.Count))
            {
                ReceiveMoney(player, item.Price, false);
                InventoryLogging.LogInventoryAction(source, this, eInventoryActionType.Other, item.Template, item.Count);
            }
		    return true;
		}

		public override bool Interact(GamePlayer player)
		{
			if(!base.Interact (player)) return false;
            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
			{
				player.Out.SendMessage("Bonjour, vous n'avez pas encore de compte.\r\nPour cr�er un compte il suffit de me donner de l'argent ou un ch�que, le compte sera cr�� automatiquement !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			string message = "Bonjour "+player.Name+", vous avez ";
			if(Money.GetMithril(bank.Money) != 0)
				message += Money.GetMithril(bank.Money)+"M ";
			if(Money.GetPlatinum(bank.Money) != 0)
				message += Money.GetPlatinum(bank.Money)+"P ";
			if(Money.GetGold(bank.Money) != 0)
				message += Money.GetGold(bank.Money)+"O ";
			if(Money.GetSilver(bank.Money) != 0)
				message += Money.GetSilver(bank.Money)+"A ";
			if(Money.GetCopper(bank.Money) != 0)
				message += Money.GetCopper(bank.Money)+"C ";
			message += "sur votre compte.\r\n";
			message += "Que voulez-vous faire ?\n\n[retirer de l'argent]\n[faire un ch�que]\n[encaisser un ch�que]";
			player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string str)
		{
			if(!base.WhisperReceive (source, str)) return false;
			GamePlayer player = source as GamePlayer;
			if (player == null)
				return true;
            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
			{
				player.Out.SendMessage("Bonjour, vous n'avez pas encore de compte.\r\nPour cr�er un compte il suffit de me donner de l'argent, le compte sera cr�� automatiquement !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			switch(str)
			{
				case "retirer de l'argent":
					player.Out.SendMessage("Combien voulez-vous retirer ?\r\n[La totalit�] [quelques pi�ces]", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
				case "La totalit�":
					WithdrawMoney(bank, player, bank.Money);
					break;
				case "quelques pi�ces":
					player.Out.SendMessage("Pour retirer quelques pi�ces, il suffit de me s�lectionner et de taper la commande /banque.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
				case "faire un ch�que":
				case "faire un cheque":
					player.Out.SendMessage("Pour faire un ch�que, il suffit de me s�lectionner et de taper la commande /banque cheque.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
				case "encaisser un ch�que":
				case "encaisser un cheque":
					player.Out.SendMessage("Pour encaisser un ch�que, il suffit de me le donner.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
			}
			return true;
		}

		/// <summary>
		/// Retire de l'argent dans la banque et le donne au joueur
		/// </summary>
		public static bool WithdrawMoney(DBBanque bank, GamePlayer player, long money)
		{
			if(bank.Money < money)
				return false;

			bank.Money -= money;
			GameServer.Database.SaveObject(bank);
			player.AddMoney(money);
			player.SaveIntoDatabase();
			return true;
		}

		/// <summary>
		/// Retire de l'argent dans la banque sans le donner au joueur
		/// </summary>
		public static bool TakeMoney(DBBanque bank, GamePlayer player, long money)
		{
			if (bank.Money < money)
				return false;

			bank.Money -= money;
			GameServer.Database.SaveObject(bank);
			return true;
		}
	}
}
