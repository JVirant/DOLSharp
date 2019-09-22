namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&show",
		ePrivLevel.Player,
		"Commands.Players.Show.Description",
		"Commands.Players.Show.Usage")]
	public class ShowCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			CardMgr.Show(client);
		}
	}
}