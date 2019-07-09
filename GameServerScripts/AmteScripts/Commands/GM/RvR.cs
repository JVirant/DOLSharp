using AmteScripts.Managers;
using AmteScripts.Utils;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&rvr",
		ePrivLevel.GM,
		"Gestion du rvr",
		"'/rvr open [region]' Force l'ouverture du rvr (ne se ferme jamais)",
        "'/rvr close' Force la fermeture du rvr",
        "'/rvr unforce' Permet après un '/rvr open' de fermer le rvr s'il n'est pas dans les bonnes horaires",
        "'/rvr refresh' Permet de rafraichir les maps disponible au rvr (voir le wiki)")]
	public class RvRCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "open":
                    ushort region = 0;
                    if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (RvrManager.Instance.Open(region, true))
                        DisplayMessage(client, "Le rvr a été ouvert avec la région " + RvrManager.Instance.Region + ".");
                    else
                        DisplayMessage(client, "Le rvr n'a pas pu être ouvert sur la région " + region + ".");
                    break;

                case "close":
                    DisplayMessage(client, RvrManager.Instance.Close() ? "Le rvr a été fermé." : "Le rvr n'a pas pu être fermé.");
                    break;

                case "unforce":
                    if (!RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Le rvr doit être ouvert pour le unforce.");
                        break;
                    }
                    RvrManager.Instance.Open(0, false);
                    DisplayMessage(client, "Le rvr sera fermé automatiquement s'il n'est plus dans les bonnes horaires.");
                    break;

                case "refresh":
                    if (RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Le rvr doit être fermé pour rafraichir la liste des maps disponibles.");
                        break;
                    }
                    string regions = "";
                    RvrManager.Instance.FindRvRMaps().Foreach(id => regions += " " + id);
                    DisplayMessage(client, "Le rvr utilise les maps:" + regions + ".");
                    break;
            }
        }
	}
}