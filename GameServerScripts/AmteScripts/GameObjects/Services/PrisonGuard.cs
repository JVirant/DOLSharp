using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class PrisonGardian : GameNPC
	{
	    public static bool Activate = true;

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			if (Activate)
			{
                if (player.Client.Account.PrivLevel >= (int)ePrivLevel.GM)
					player.Out.SendMessage("(Menu GM) [Désactivation Gardien]", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			    var objs = from p in JailMgr.PlayerXPrisoner
			               where p.Value.RP
                           select p;
				if (objs.Count() > 0)
				{
					player.Out.SendMessage("Pour quelques pieces d'or, je peux libérer les prisoniers ...\n Voici la liste des prisoniers:", eChatType.CT_System, eChatLoc.CL_PopupWindow);

					int nb_prisoners = 0;
                    foreach (KeyValuePair<GamePlayer, Prisoner> kp in objs)
                    {
                        string textePrisonier = "[" + kp.Key.Name + "] (Coût: " + kp.Value.Cost + " or)";
                        player.Out.SendMessage(textePrisonier, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        nb_prisoners++;
                    }
				    if (nb_prisoners == 0)
						player.Out.SendMessage("Désolé, il n'y a aucun prisonier dans ce monde actuellement.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				}
				else
					player.Out.SendMessage("Il n'y a pas de prisonier actuellement.", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			}
			else
			{
                if (player.Client.Account.PrivLevel >= (int)ePrivLevel.GM)
					player.Out.SendMessage("(Menu GM) [Activation Gardien]", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				player.Out.SendMessage("Vous souhaitez y entrer, vous aussi ?", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
		    GamePlayer player = source as GamePlayer;
		    if (!base.WhisperReceive(source, text) || player == null)
		        return false;

		    if (player.Client.Account.PrivLevel >= (int) ePrivLevel.GM)
		    {
		        switch (text)
		        {
		            case "Activation Gardien":
		                Activate = true;
		                player.Out.SendMessage("Gardien actif.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		                return true;
		            case "Désactivation Gardien":
					case "Desactivation Gardien":
		                Activate = false;
		                player.Out.SendMessage("Gardien inactif.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		                return true;
		        }
		    }

		    GamePlayer gameprisoner = WorldMgr.GetClientByPlayerName(text, true, true).Player;
		    if (gameprisoner == null)
		    {
                player.Out.SendMessage("Ce prisonier est introuvable.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		        return true;
		    }
		    Prisoner prisoner = JailMgr.GetPrisoner(gameprisoner);
            if (prisoner == null)
            {
                player.Out.SendMessage("Ce prisonier est introuvable.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

		    int PrixTotal = prisoner.Cost;
		    if (player.Client.Account.PrivLevel == 1 && !player.RemoveMoney(PrixTotal*10000))
		    {
		        player.Out.SendMessage("Vous n'avez pas assez d'argent...", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		        return true;
		    }

		    JailMgr.Relacher(gameprisoner);
		    player.Out.SendMessage("Vous venez de libérer " + text + " pour " + PrixTotal + " or.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		    return true;
		}
	}
}