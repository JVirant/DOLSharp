using System;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class Copyist : AmteMob
	{
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			player.Client.Out.SendMessage("Bonjour ! Que puis-je faire pour vous ?\n" +
			                              "Pour 2 pieces d'or, je peux dupliquer un livre dont vous êtes l'auteur !",
			                              eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			GamePlayer p = source as GamePlayer;
			if (p == null || item == null)
				return false;

			if (item.Id_nb.StartsWith("scroll"))
			{
				var book = GameServer.Database.SelectObject<DBBook>("Name = '" + GameServer.Database.Escape(item.Name) + "'");
				if (book != null)
				{
					if (book.PlayerID != p.InternalID)
					{
						p.Out.SendMessage("\"Désolé, seul l'auteur d'un livre peut demander une copie.\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);
						return false;
					}
					if (!p.RemoveMoney(20000))
					{
						p.Out.SendMessage("\"Il vous faut 5 piece d'or pour dupliquer un livre.\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);
						return false;
					}
					//p.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(item.Template));
					var iu = new ItemUnique(item.ITWrapper) {Id_nb = "scroll" + Guid.NewGuid()};
					GameServer.Database.AddObject(iu);
					p.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(iu));
					p.Out.SendMessage("\"Voilà la copie de votre livre sir " + p.Name + ".\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				}
				else
					p.Out.SendMessage("\"Désolé, ce livre n'existe plus.\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			}
			else
				p.Out.SendMessage("\"Désolé, je ne peux recopier qu'un livre ...\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			return false;
		}
	}
}
