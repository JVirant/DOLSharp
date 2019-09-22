using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&namemount",
		ePrivLevel.Player,
		"Commands.Players.Namemount.Description",
		"Commands.Players.Namemount.Usage")]
	public class NameHorseCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player == null || args.Length < 2)
				return;

			if (IsSpammingCommand(client.Player, "namemount"))
				return;

			string horseName = args[1];
			if (!client.Player.HasHorse)
				return;
			client.Player.ActiveHorse.Name = horseName;
		}
	}
}