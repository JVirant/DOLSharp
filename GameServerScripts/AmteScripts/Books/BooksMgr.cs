using System;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Events;
using System.Reflection;
using log4net;

namespace DOL.GS.Scripts
{
    public class BooksMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
			GameEventMgr.AddHandler(GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseSlot));
        }
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
			GameEventMgr.RemoveHandler(GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseSlot));
        }

        protected static void PlayerUseSlot(DOLEvent e, object sender, EventArgs args)
        {
			GamePlayer player = sender as GamePlayer;
            if (player == null) return;

            UseSlotEventArgs uArgs = (UseSlotEventArgs)args;
            
            InventoryItem item = player.Inventory.GetItem((eInventorySlot) uArgs.Slot);
            if(item == null) return;

        	if (item.Id_nb.StartsWith("scroll"))
        		ReadBook(player, GameServer.Database.SelectObject<DBBook>("ID ='" + item.MaxCondition + "'"));
        }

		public static void ReadBook(GamePlayer player, DBBook dbBook)
		{
			if (dbBook == null)
			{
				player.Client.Out.SendMessage("~~ Parchemin Vierge ~~", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				return;
			}

			var sb = new StringBuilder(2048);
			sb
				.Append("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n")
				.Append("Auteur: ").Append(dbBook.Author).Append("\n")
				.Append("Titre: ").Append(dbBook.Title).Append("\n")
				.Append("Encre utilisée: " + dbBook.Ink + "\n")
				.Append("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

			player.Client.Out.SendMessage(sb.ToString(), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			sb.Clear();

			for (int i = 0; i < dbBook.Text.Length; i++)
			{
				if (i + 2 < dbBook.Text.Length)
					if ((dbBook.Text[i] == '\n') && (dbBook.Text[i + 1] == '\n'))
					{
						player.Client.Out.SendMessage(sb.ToString(), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						sb.Clear();
						i++;
						i++;
						continue;
					}
					else if (sb.Length > 1900)
					{
						player.Client.Out.SendMessage(sb.ToString(), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						sb.Clear();
					}
				sb.Append(dbBook.Text[i]);
			}
			player.Client.Out.SendMessage(sb.ToString(), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
		}
    }
}
