using System.Linq;
using System.Text;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class Librarian : AmteMob
	{
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			player.Client.Out.SendMessage("Bonjour et bienvenue à la bibliothèque d'Amtenaël !\n" +
										  "Que voulez-vous ?\n\n" +
										  "[Voir les livres]\n[Ajouter un livre]",
										  eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			var player = source as AmtePlayer;
			if (!base.WhisperReceive(source, text) || player == null)
				return false;

			switch (text)
			{
				case "Voir les livres":
					player.SendMessage("Voici la liste des livres que je détiens:", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					StringBuilder sb = new StringBuilder(2048);
					GameServer.Database.SelectObjects<DBBook>("IsInLibrary = 1").OrderBy(b => b.Title).Foreach(
						b =>
						{
							sb.Append("\n[").AppendLine(b.Title).Append("] de ").Append(b.Author);
							if (sb.Length > 1900)
							{
								player.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
								sb.Clear();
							}
						});
					player.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;

				case "Ajouter un livre":
					player.SendMessage("Donnez moi votre livre et je l'ajouterais.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;

				default:
					var book = GameServer.Database.SelectObject<DBBook>("Title = '" + GameServer.Database.Escape(text) + "' AND IsInLibrary = 1");
					if (book == null)
					{
						player.SendMessage("Je ne trouve pas ce livre.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
						break;
					}
					BooksMgr.ReadBook(player, book);
					break;
			}
			return true;
		}

		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			var p = source as GamePlayer;
			if (p == null || item == null)
				return false;

			if (item.Id_nb.StartsWith("scroll"))
			{
				var book = GameServer.Database.SelectObject<DBBook>("Name = '" + GameServer.Database.Escape(item.Name) + "'");
				if (book != null)
				{
					if (book.PlayerID != p.InternalID)
					{
						p.Out.SendMessage("Vous n'êtes pas l'auteur de ce livre !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
						return false;
					}
					book.IsInLibrary = !book.IsInLibrary;
					book.Save();
					p.Out.SendMessage(
						book.IsInLibrary
							? "Votre livre fait maintenant partit de la bibliothèque."
							: "Vous avez retiré votre livre de la bibliothèque.", eChatType.CT_System,
						eChatLoc.CL_PopupWindow);
				}
				else
					p.Out.SendMessage("Désolé, ce livre n'existe plus.", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			}
			else
				p.Out.SendMessage("Qu'est-ce que c'est ?!", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			return false;
		}
	}
}
