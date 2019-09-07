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

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&formation",
		ePrivLevel.Player,
		"Commands.Players.Formation.Description",
		"Commands.Players.Formation.Usage")]
	public class FormationHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "formation"))
				return;

			GamePlayer player = client.Player;

			//No one else needs to use this spell
			if (player.CharacterClass.ID != (int)eCharacterClass.Bonedancer)
			{
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Formation.OnlyBonedancer"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				return;
			}

			//Help display
			if (args.Length == 1)
			{
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Formation.Usage.Formation"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Formation.Usage.Triangle"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Formation.Usage.Line"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Formation.Usage.Protect"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				return;
			}

			//Check to see if the BD has a commander and minions
			if (player.ControlledBrain == null)
			{
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						"Commands.Players.Formation.Commander.Missing"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				return;
			}
			bool haveminion = false;
			foreach (AI.Brain.IControlledBrain icb in player.ControlledBrain.Body.ControlledNpcList)
			{
				if (icb != null)
				{
					haveminion = true;
					break;
				}
			}
			if (!haveminion)
			{
				client.Out.SendMessage(
					LanguageMgr.GetTranslation(
						"Commands.Players.Formation.Minion.Missing"
					),
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
				);
				return;
			}

			switch (args[1].ToLower())
			{
				//Triangle Formation
				case "triangle":
					player.ControlledBrain.Body.Formation = GameNPC.eFormationType.Triangle;
					break;
				//Line formation
				case "line":
					player.ControlledBrain.Body.Formation = GameNPC.eFormationType.Line;
					break;
				//Protect formation
				case "protect":
					player.ControlledBrain.Body.Formation = GameNPC.eFormationType.Protect;
					break;
				default:
					client.Out.SendMessage(
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Formation.Argument.Error",
							args[1]
						),
						eChatType.CT_System,
						eChatLoc.CL_SystemWindow
					);
					break;
			}
		}
	}
}