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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&whisper",
		new string[] {"&whis"}, //Important, don't remove this alias, its used for communication with mobs!
		ePrivLevel.Player,
		"Commands.Players.Whisper.Description",
		"Commands.Players.Whisper.Usage")]
	public class WhisperCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			if (IsSpammingCommand(client.Player, "whisper", 500))
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Whisper.SlowDown"));
				return;
			}

			GameObject obj = client.Player.TargetObject;
			if (obj == null)
			{
				DisplayMessage(client,
				LanguageMgr.GetTranslation(
					client.Account.Language,
					"Commands.Players.Whisper.SelectTarget"));
				return;
			}

			/*if (obj is GameLiving == false)
			{
				DisplayMessage(client, "You look pretty silly whispering to " + obj.GetName(0, false) + ".");
				return;
			}*/

			if (obj == client.Player)
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Whisper.NotYourself"));
				return;
			}
			client.Player.Whisper(obj, string.Join(" ", args, 1, args.Length - 1));
		}
	}
}