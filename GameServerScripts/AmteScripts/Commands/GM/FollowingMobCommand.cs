using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&followmob",
		ePrivLevel.GM,
		"FollowMob Commands",
		"/followmob create - créé un FollowMob",
        "/followmob follow <nom du mob à suivre> - Regle la cible que le mob doit suivre",
		"/followmob copy - Copie le mob avec la cible déjà reglée")]
	public class FollowMobCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
			FollowingMob mob = client.Player.TargetObject as FollowingMob;
			switch (args[1])
			{
                case "create":
					mob = new FollowingMob();
                    mob.X = client.Player.X;
                    mob.Y = client.Player.Y;
                    mob.Z = client.Player.Z;
                    mob.Heading = client.Player.Heading;
                    mob.CurrentRegion = client.Player.CurrentRegion;
                    mob.Name = "New Following Mob";
                    mob.Model = 407;
                    mob.LoadedFromScript = false;
                    mob.AddToWorld();
                    mob.SaveIntoDatabase();
                    client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "follow":
                    if (mob == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }
					mob.FollowMobID = "";
					mob.MobFollow = null;
					mob.StopFollowing();
					foreach (GameNPC npc in mob.GetNPCsInRadius(2000))
						if (npc.Name == args[2])
						{
							mob.MobFollow = npc;
							mob.FollowMobID = npc.InternalID;
							mob.Follow(npc, 10, 3000);
							break;
						}
					if(mob.MobFollow == null)
					{
						client.Out.SendMessage("Aucun mob '" + mob.MobFollow.Name + "' trouvé.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						break;
					}
                    mob.SaveIntoDatabase();
                    client.Out.SendMessage("Le mob suit maintenant '" + mob.MobFollow.Name + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "copy":
                    if (mob == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }
					FollowingMob oldmob = mob;
					mob = new FollowingMob();
					mob.X = client.Player.X;
					mob.Y = client.Player.Y;
					mob.Z = client.Player.Z;
					mob.Heading = client.Player.Heading;
					mob.CurrentRegion = client.Player.CurrentRegion;
					
					mob.Level = oldmob.Level;
					mob.Realm = oldmob.Realm;
					mob.Name = oldmob.Name;
					mob.Model = oldmob.Model;
					mob.Flags = oldmob.Flags;
					mob.MeleeDamageType = oldmob.MeleeDamageType;
					mob.RespawnInterval = oldmob.RespawnInterval;
					mob.CurrentSpeed = 0;
					mob.MaxSpeedBase = oldmob.MaxSpeedBase;
					mob.GuildName = oldmob.GuildName;
					mob.Size = oldmob.Size;
					mob.Inventory = oldmob.Inventory;
					mob.FollowMobID = oldmob.FollowMobID;
					mob.MobFollow = oldmob.MobFollow;
					mob.Follow(mob.MobFollow, 10, 3000);
					
					mob.AddToWorld();
					mob.SaveIntoDatabase();
					client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

                default:
                    DisplaySyntax(client);
                    break;
			}
		}
	}
}