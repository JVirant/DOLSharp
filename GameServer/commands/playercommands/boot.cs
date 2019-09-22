using DOL.GS.Housing;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	  "&boot",
	  ePrivLevel.Player,
	  "Commands.Players.Boot.Description",
	  "Commands.Players.Boot.Usage")]
	public class BootCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "boot"))
				return;

            House house = client.Player.CurrentHouse;
			if (house == null)
			{
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boot.InHouseError"));
				return;
			}

			// no permission to banish, return
			if (!house.CanBanish(client.Player))
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boot.NoPermission"));
				return;
			}

			// check each player, try and find player with the given name (lowercase cmp)
			foreach (GamePlayer player in house.GetAllPlayersInHouse())
			{
				if (player != client.Player && player.Name.ToLower() != args[1].ToLower())
				{
					ChatUtil.SendSystemMessage(client, "Commands.Players.Boot.YouRemoved", client.Player.Name);
					player.LeaveHouse();

					return;
				}
			}

			ChatUtil.SendHelpMessage(client, "Commands.Players.Boot.NoOneOnline", null);
		}
	}
}