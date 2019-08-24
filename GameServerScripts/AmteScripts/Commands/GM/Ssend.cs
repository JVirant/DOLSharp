using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&ssend",
		ePrivLevel.GM,
		"Répondre aux report",
		"/ssend <nick> <message>")]
	public class SsendCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 2)
            {
                DisplaySyntax(client);
                return;
            }

            GameClient cl_player = WorldMgr.GetClientByPlayerName(args[1], false, true);
            string message = string.Join(" ", args, 2, args.Length - 2);
			cl_player.Out.SendMessage("[Report] " + client.Player.Name + ": " + message, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
			foreach (var cl in WorldMgr.GetAllPlayingClients())
				if (cl.Account.PrivLevel >= 2 && cl != cl_player)
					cl.Out.SendMessage("[Report] " + client.Player.Name + " à " + cl_player.Player.Name + ": \"" + message + "\".", eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
        }
	}
}