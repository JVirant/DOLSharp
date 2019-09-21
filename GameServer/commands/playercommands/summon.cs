using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&summon",
		ePrivLevel.Player,
		"Commands.Players.Summon.Description",
		"Commands.Players.Summon.Usage")]
	public class SummonHorseCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player == null)
				return;
			try
			{
				if (args.Length > 1 && Convert.ToInt16(args[1]) == 0)
					client.Player.IsOnHorse = false;
			}
			catch
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Summon.Incorrect"));
			}
			finally
			{
				if (client.Player.Inventory.GetItem(eInventorySlot.Horse) != null)
					client.Player.UseSlot(eInventorySlot.Horse, eUseType.clic);
			}
		}
	}
}