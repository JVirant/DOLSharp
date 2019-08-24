using DOL.GS.Scripts;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		 "&place",
		 ePrivLevel.GM,
		 "Commande pour les places assises",
		 "'/place create' Pour créer une place assise")]
	public class PlaceAssiseCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			switch (args[1].ToLower())
			{
				case "create":
					PlaceAssise p = new PlaceAssise();
					p.X = client.Player.X;
					p.Y = client.Player.Y;
					p.Z = client.Player.Z;
					p.CurrentRegion = client.Player.CurrentRegion;
					p.Heading = client.Player.Heading;
					p.AddToWorld();
					p.SaveIntoDatabase();
					break;

				default:
					DisplaySyntax(client);
					break;
			}
		}
	}
}
