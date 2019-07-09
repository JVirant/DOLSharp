/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.GS.Commands;
using System.Collections.Generic;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		"&nightmob",
		ePrivLevel.GM,
		"NightMob Commands",
		"'/nightmob create' - créé un NightMob",
	    "'/nightmob info' - affiche les infos du NightMob ciblé",
	    "'/nightmob time <start> <end>' - change les heures d'apparition du mob")]
	public class NightMobCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
            NightMob mob = client.Player.TargetObject as NightMob;
			switch (args[1].ToLower())
			{
                case "create":
                    mob = new NightMob();
                    mob.X = client.Player.X;
                    mob.Y = client.Player.Y;
                    mob.Z = client.Player.Z;
                    mob.Heading = client.Player.Heading;
                    mob.CurrentRegion = client.Player.CurrentRegion;
					mob.Flags = GameNPC.eFlags.PEACE;
                    mob.Name = "New Night Mob";
                    mob.Model = 409;
                    mob.LoadedFromScript = false;
                    mob.AddToWorld();
                    mob.SaveIntoDatabase();
                    client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "info":
                    if (mob == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    IList<string> txt = new List<string>();
                    txt.Add("Apparition entre " + mob.StartHour + ":00 et " + mob.EndHour + ":00.");
                    client.Out.SendCustomTextWindow(mob.Name + " Info", txt);
                    break;

				case "hour":
                case "time":
                    if (mob == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }
					try
					{
						mob.StartHour = int.Parse(args[2]);
						mob.EndHour = int.Parse(args[3]);
						//check 
						if (mob.StartHour > 24)
							mob.StartHour = 24;
						if (mob.EndHour > 24)
							mob.EndHour = 24;
						if (mob.EndHour == 24 && mob.StartHour == 24)
							mob.StartHour = 0;
						if (mob.StartHour < 0)
							mob.StartHour = 0;
						if (mob.EndHour < 0)
							mob.EndHour = 0;
					}
					catch
					{
						DisplaySyntax(client);
                        return;
					}
					mob.SaveIntoDatabase();
					client.Out.SendMessage("Les heures d'apparition sont maintenant " + mob.StartHour + ":00 et " + mob.EndHour + ":00.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                default: DisplaySyntax(client); break;
			}
			return;
		}
	}
}