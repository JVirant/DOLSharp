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
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		 "&safety",
		 ePrivLevel.Player,
		 "Commands.Players.Safety.Description",
		 "Commands.Players.Safety.Usage")]
	public class SafetyCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player.IsPvP == false)
				return;

			if(args.Length >= 2 && args[1].ToLower() == "off")
			{
				client.Player.SafetyFlag = false;
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Safety.Off"));
			}
			else if(client.Player.SafetyFlag)
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Message.1"));
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Message.2"));
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Message.3"));
			}
			else
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Safety.Off.Already"));
			}
		}
	}
}