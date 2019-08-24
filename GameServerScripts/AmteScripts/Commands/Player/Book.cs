using System;
using System.Reflection;
using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		 "&book",
		 ePrivLevel.Player,
		 "Créer un livre vierge",
		 "/book <titre>")]
	public class BookCommandHandler : ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public void OnCommand(GameClient client, string[] args)
		{
			GamePlayer player = client.Player;
			try
			{
				if (args.Length < 2)
				{
					Aide(player);
					return;
				}

				string ScrollTitle = args[2];

				DBBook theScroll = null;

				switch (args[1])
				{
					case "write":
					case "remove":
					case "correct":
						// Selection du livre
						theScroll = GetBookFromTitle(ScrollTitle);
						if (theScroll == null)
						{
							player.Out.SendMessage("Le livre \"" + ScrollTitle + "\" n'existe pas !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							return;
						}

						// Verification des droits
						if (!isAuthor(player, theScroll))
							return;
						break;
				}

				switch (args[1])
				{
					#region Création
					case "create":

						ScrollTitle = String.Join(" ", args, 2, args.Length - 2);
						var item = player.Inventory.GetItem(eInventorySlot.LastBackpack);

						if ((item.Id_nb != "scroll") || (item.Name != "Parchemin vierge"))
						{
							player.Out.SendMessage("Vous devez posseder un parchemin vierge dans le dernier emplacement de votre inventaire pour créer un livre.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							return;
						}

						var book = GameServer.Database.SelectObject<DBBook>("Title = '" + GameServer.Database.Escape(ScrollTitle) + "'");
						if (book != null)
						{
							player.Out.SendMessage("Ce livre existe déjà.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							return;
						}

						if (!player.Inventory.RemoveItem(item))
							return;

						theScroll = new DBBook
									{
										Name = "[" + player.Name + "] " + ScrollTitle,
										Title = ScrollTitle,
										Author = player.Name,
										Text = "",
										PlayerID = player.InternalID,
										Ink = "",
										InkId = "",
									};
						theScroll.Save();

						var iu = new ItemUnique(item.Template)
						         {
						         	Name = "[" + player.Name + "] " + ScrollTitle,
						         	Model = 498,
						         	MaxCondition = (int) theScroll.ID
						         };
						GameServer.Database.AddObject(iu);
						player.Inventory.AddItem(eInventorySlot.LastBackpack, GameInventoryItem.Create(iu));
						player.Out.SendMessage("Vous créez un livre.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						break;
					#endregion
					#region Ecriture
					case "write":
						if (!HaveFeather(player) || !HaveInk(player) || !HaveRightInk(player, theScroll.InkId))
							return;

						theScroll.Ink = (theScroll.Ink == "") ? GetInkType(player) : theScroll.Ink;
						theScroll.InkId = (theScroll.InkId == "") ? GetInkId(player) : theScroll.InkId;

						theScroll.Text += string.Join(" ", args, 3, args.Length - 3);
						theScroll.Text += "\n";

						DecInk(player, theScroll.InkId);
						theScroll.Save();
						player.Out.SendMessage("Vous ecrivez quelques lignes sur \"" + ScrollTitle + "\".", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						break;
					#endregion
					#region Suppression
					case "remove":
						for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
						{
							InventoryItem itm = player.Inventory.GetItem(i);
							if (itm != null && itm.Name == theScroll.Name)
							{
								player.Inventory.RemoveCountFromStack(itm, itm.Count);
								InventoryLogging.LogInventoryAction(player, "(null)", eInventoryActionType.Other, itm.Template, itm.Count);
							}
						}
						GameServer.Database.DeleteObject(theScroll);
						player.Out.SendMessage("Vous brûlez avec peine le dernier exemplaire de l'oeuvre nommée \"" + ScrollTitle + "\"...", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						break;
					#endregion
					#region Correction
					case "correct":
						if (theScroll.Text.IndexOf("\n") == -1)
						{
							player.Out.SendMessage("Vous ne pouvez pas effacer un parchemin vide !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							return;
						}
						if (!HaveAcid(player))
							return;

						theScroll.Text = theScroll.Text.Substring(0, theScroll.Text.Length - 2);
						theScroll.Text = theScroll.Text.Substring(0, theScroll.Text.LastIndexOf('\n') + 1);

						theScroll.Save();
						player.Out.SendMessage("Vous effacez la dernière ligne de \"" + ScrollTitle + "\".", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						break;
					#endregion
				}

			}
			catch (Exception e)
			{
				log.Info("/book Exception: " + e);
				Aide(player);
			}
		}


		public DBBook GetBookFromTitle(string ScrollTitle)
		{
			return GameServer.Database.SelectObject<DBBook>("Title ='" + GameServer.Database.Escape(ScrollTitle) + "'");
		}

		/// <summary>
		/// Retourne le type d'encre
		/// </summary>
		public string GetInkType(GamePlayer player)
		{
			string ItemName;
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null)
				{
					ItemName = player.Inventory.GetItem(i).Id_nb;
					if ((ItemName.StartsWith("ink_")) || (ItemName.StartsWith("blood_")))
						return player.Inventory.GetItem(i).Name.Replace("(Special) ", "");
				}
			return "Encre Inconnue";
		}

		/// <summary>
		/// Retourne le type d'encre
		/// </summary>
		public string GetInkId(GamePlayer player)
		{
			string itemId;
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null)
				{
					itemId = player.Inventory.GetItem(i).Id_nb;
					if ((itemId.StartsWith("ink_")) || (itemId.StartsWith("blood_")))
						return itemId;
				}
			return "Encre Inconnue";
		}

		/// <summary>
		/// Baisse le niveau d'encre de la fiole
		/// </summary>
		public void DecInk(GamePlayer player, string ink)
		{
			InventoryItem item;
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
			{
				item = player.Inventory.GetItem(i);
				if (item != null && item.Id_nb == ink)
				{
					--item.Condition;
					if (item.Condition <= 0)
					{
						player.Inventory.RemoveCountFromStack(item, item.Count);
						InventoryLogging.LogInventoryAction(player, "(null)", eInventoryActionType.Other, item.Template, item.Count);
					}
					else
						player.Client.Out.SendInventoryItemsUpdate(new[] { item });
					break;
				}
			}
		}

		/// <summary>
		/// Retourne true si player est l'auteur du livre
		/// </summary>
		public bool isAuthor(GamePlayer player, DBBook theScroll)
		{
			if (theScroll.PlayerID != player.InternalID)
			{
				player.Out.SendMessage("Vous n'etes pas l'auteur de \"" + theScroll.Title + "\".", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Retourne true si le joueur a une plume
		/// </summary>
		public bool HaveFeather(GamePlayer player)
		{
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null)
					if (player.Inventory.GetItem(i).Id_nb == "feather" ||
						player.Inventory.GetItem(i).Id_nb.StartsWith("feather_"))
						return true;
			player.Out.SendMessage("Vous devez posseder une plume d'écrivain pour ecrire sur un parchemin.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			return false;
		}

		/// <summary>
		/// Retourne true si le joueur a de l'encre
		/// </summary>
		public bool HaveInk(GamePlayer player)
		{
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null)
					if (player.Inventory.GetItem(i).Id_nb.StartsWith("ink_") ||
						player.Inventory.GetItem(i).Id_nb.StartsWith("blood_"))
						return true;
			player.Out.SendMessage("Vous devez posseder de l'encre ou du sang pour ecrire sur un parchemin.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			return false;
		}

		/// <summary>
		/// Retourne true si le joueur a de l'acide
		/// </summary>
		public bool HaveAcid(GamePlayer player)
		{
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null)
					if (player.Inventory.GetItem(i).Id_nb == "corrector")
						return true;
			player.Out.SendMessage("Vous devez posseder un correcteur pour effacer les lignes d'un parchemin.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			return false;
		}

		/// <summary>
		/// Retourne true si le joueur a la bonne encre
		/// </summary>
		public bool HaveRightInk(GamePlayer player, string ink)
		{
			if (String.IsNullOrWhiteSpace(ink))
				return true;
			for (var i = eInventorySlot.FirstBackpack; i <= eInventorySlot.LastBackpack; i++)
				if (player.Inventory.GetItem(i) != null &&
				    player.Inventory.GetItem(i).Id_nb == ink)
					return true;
			player.Out.SendMessage("Vous devez posseder de l'encre de type \"" + ink + "\" pour continuer l'écriture de ce parchemin.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			return false;
		}

		/// <summary>
		/// Affiche l'aide au joueur
		/// </summary>
		public void Aide(GamePlayer player)
		{
			player.Out.SendMessage("Utilisation du /book :", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("- /book create \"Titre\" : Crée un livre à partir d'un parchemin vierge. (En dernier slot de l'inventaire)", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("- /book write \"Titre\" <Texte à écrire> : Ecrit du texte sur un parchemin.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("-- (Si vous possedez plusieurs type d'encre, la première de votre inventaire sera utilisée)", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("- /book remove \"Titre\" : Détruire totalement un livre. (Auteur seulement)", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("- /book correct \"Titre\" : Efface la dernière ligne d'un parchemin.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage("- /use (Apres clic droit sur le livre) : Ouvre une fenetre de lecture.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
	}
}