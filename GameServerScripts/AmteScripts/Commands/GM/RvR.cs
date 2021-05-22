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
		"'/rvr openpvp [region]' Force l'ouverture du pvp (ne se ferme jamais)",
		"'/rvr closepvp' Force la fermeture du pvp",
		"'/rvr unforcepvp' Permet après un '/rvr openpvp' de fermer le pvp s'il n'est pas dans les bonnes horaires",
		"'/rvr refresh' Permet de rafraichir les maps disponible au rvr et au pvp")]
	public class RvRCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length <= 1)
			{
				DisplaySyntax(client);
				return;
			}

			ushort region = 0;
			switch (args[1].ToLower())
			{
				case "open":
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
				case "openpvp":
					if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
					{
						DisplaySyntax(client);
						return;
					}
					if (PvpManager.Instance.Open(region, true))
						DisplayMessage(client, "Le pvp a été ouvert avec la région " + PvpManager.Instance.Region + ".");
					else
						DisplayMessage(client, "Le pvp n'a pas pu être ouvert sur la région " + region + ".");
					break;

				case "close":
					DisplayMessage(client, RvrManager.Instance.Close() ? "Le rvr a été fermé." : "Le rvr n'a pas pu être fermé.");
					break;

				case "closepvp":
					DisplayMessage(client, PvpManager.Instance.Close() ? "Le pvp a été fermé." : "Le pvp n'a pas pu être fermé.");
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

				case "unforcepvp":
					if (!PvpManager.Instance.IsOpen)
					{
						DisplayMessage(client, "Le pvp doit être ouvert pour le unforce.");
						break;
					}
					PvpManager.Instance.Open(0, false);
					DisplayMessage(client, "Le pvp sera fermé automatiquement s'il n'est plus dans les bonnes horaires.");
					break;

				case "refresh":
					if (RvrManager.Instance.IsOpen || PvpManager.Instance.IsOpen)
					{
						DisplayMessage(client, "Le rvr et le pvp doivent être fermés pour rafraichir la liste des maps disponibles.");
						break;
					}
					var rvr = string.Join(", ", RvrManager.Instance.FindRvRMaps());
					var pvp = string.Join(", ", PvpManager.Instance.FindPvPMaps());
					DisplayMessage(client, $"Le rvr utilise les maps: {rvr}, le pvp utilise les maps: {pvp}.");
					break;
			}
		}
	}
}
