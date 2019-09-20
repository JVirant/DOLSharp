namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&held",
		ePrivLevel.Player,
		"Commands.Players.Held.Description",
		"Commands.Players.Held.Usage")]
	public class HeldCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
            if (args.Length == 2 && client.Player.Group != null)
                foreach (GamePlayer Groupee in client.Player.Group.GetPlayersInTheGroup())
                    if(Groupee != client.Player) CardMgr.Held(client, Groupee.Client);
            else
                CardMgr.Held(client, client);
		}
	}
}