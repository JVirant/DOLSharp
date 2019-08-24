using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using System.Collections.Generic;
using DOL.GS.Scripts;
using DOL.Events;
using System;

namespace DOL.GS
{
    public static class AmteUtils
    {
        /// <summary>C'est le serveur test ?</summary>
        public static bool IsTestServer
        {
            get
            {
                return GameServer.Instance.Configuration.ServerNameShort == "AMTETEST";
            }
        }

        /// <summary>C'est le serveur de prod ?</summary>
        public static bool IsLiveServer
        {
            get
            {
                return GameServer.Instance.Configuration.ServerNameShort == "AMTENAEL";
            }
        }

        /// <summary>
        /// Efface le contenu de la popup IG
        /// </summary>
        /// <param name="player"></param>
		public static void SendClearPopupWindow(GamePlayer player)
		{
			GameObject obj = player.TargetObject;
			player.Out.SendChangeTarget(player);
			player.Out.SendMessage("", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			player.Out.SendChangeTarget(obj);
		}

        /// <summary>Supprime une guilde du serveur</summary>
        private static void _OnGuildRemoved()
        {

        }

        [ScriptLoadedEvent]
        private static void _OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            //TODO: évènement sur la suppression d'une guilde => appel _OnGuildRemoved
        }
    }
}
