using DOL.GS;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&deal",
		ePrivLevel.Player,
		"Commands.Players.Deal.Description",
		"Commands.Players.Deal.Usage")]
	public class DealCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 3)
				return;

            bool up = false;

			if (args[2][0] == 'u')
				up = true;
			else if
				(args[2][0] == 'd') up = false;
			else
				return;

			GameClient friendClient = WorldMgr.GetClientByPlayerName(args[1], true, true);
            CardMgr.Deal(client, friendClient, up);
		}
	}
}