using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class Geolier : GameNPC
	{
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			TurnTo(player);
			
			string message;
			if (JailMgr.IsPrisoner(player))
				message = "Bonjour " + player.Name + ", tu veux  connaitre ta [peine] de prison ?!";
			else
				message = "Vous voulez aller en prison ?";
			player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str)) return false;
			if (!(source is GamePlayer)) return false;
			
			GamePlayer player = source as GamePlayer;
			TurnTo(player);
			
			switch(str)
			{
				case "peine":
					Prisoner prison = JailMgr.GetPrisoner(player);
					if(prison == null) return Interact(player);
					if (prison.RP)
						player.Out.SendMessage("Attends le " + prison.Sortie.ToShortDateString() + " vers " + prison.Sortie.Hour + "h ou demande à quelqu'un de payer ta caution.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					else
						player.Out.SendMessage("Attends le " + prison.Sortie.ToShortDateString() + " vers " + prison.Sortie.Hour + "h.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					break;
					
				default: return Interact(player);
			}
			return true;
		}
	}
}
