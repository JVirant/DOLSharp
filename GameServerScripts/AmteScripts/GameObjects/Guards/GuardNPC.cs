using System;
using System.Text;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public interface IGuardNPC
	{
		
	}

	public class GuardNPC : AmteMob, IGuardNPC
    {
        public GuardNPC()
        {
            SetOwnBrain(new GuardNPCBrain());
        }

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player) || BlacklistMgr.IsBlacklisted((AmtePlayer)player))
				return false;

			player.Out.SendMessage("Bonjour, que voulez-vous ?\n\n[Signaler] mon tueur !\n[Voir] la liste noire.",
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			var player = source as AmtePlayer;
			if (!base.WhisperReceive(source, text) || player == null || BlacklistMgr.IsBlacklisted(player))
				return false;

			switch (text)
			{
				case "Signaler":
					if (BlacklistMgr.ReportPlayer(player))
						player.Out.SendMessage("La personne qui vous a tué a été signalé !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					else
						player.Out.SendMessage("C'est trop tard pour signaler votre tueur !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;

				case "Voir":
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("Les personnes suivantes sont sur la liste noire:");
					BlacklistMgr.GetBlacklistedNames().ForEach(s => sb.AppendLine(s));
					player.Out.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
			}
			return true;
		}

		public override bool ReceiveItem(GameLiving source, Database.InventoryItem item)
		{
			if (!(source is AmtePlayer) || item == null || !item.Id_nb.StartsWith(BlacklistMgr.HeadTemplate.Id_nb))
				return false;
			var player = (AmtePlayer)source;

			if (!item.CanDropAsLoot)
			{
				player.Out.SendMessage("Hmm, peut-être que... non, ça ne me dit rien !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (new DateTime(2000, 1, 1).Add(new TimeSpan(0, 0, item.MaxCondition)) < DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)))
			{
				player.Out.SendMessage("Elle a l'air pourri cette tête, je ne la reconnais pas !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (!player.Inventory.RemoveCountFromStack(item, 1))
				return false;

			BlacklistMgr.GuardReportBL(player, item.IUWrapper.MessageArticle);
			player.Out.SendMessage("Merci de votre précieuse aide !", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			return true;
		}

		public override void WalkToSpawn(short speed)
		{
			base.WalkToSpawn(MaxSpeed);
		}
	}

	public class GuardTextNPC : TextNPC, IGuardNPC
    {
        public GuardTextNPC()
        {
            SetOwnBrain(new GuardNPCBrain());
        }
    }
}
