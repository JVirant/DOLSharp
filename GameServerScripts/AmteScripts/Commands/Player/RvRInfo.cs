using AmteScripts.Managers;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&rvrinfo",
        new[] { "/score" },
		ePrivLevel.Player,
		"Avoir des informations sur le rvr",
		"'/rvrinfo' Donne des informations à propos du rvr")]
	public class RvRInfoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "rvrinfo", 500))
            {
                DisplayMessage(client, "Arrête de spammer la commande ! Tu vas te faire mal aux doigts !");
                return;
            }

            client.Out.SendCustomTextWindow("RvR", RvrManager.Instance.GetStatistics());
        }
	}
}