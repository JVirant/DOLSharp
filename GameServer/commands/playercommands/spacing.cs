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
		"&spacing",
		ePrivLevel.Player,
		"Commands.Players.Spacing.Description",
		"Commands.Players.Spacing.Usage")]
	public class SpacingHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "spacing"))
				return;

			GamePlayer player = client.Player;

			//No one else needs to use this spell
			if (player.CharacterClass.ID != (int)eCharacterClass.Bonedancer)
			{
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Only.Bone"));
				return;
			}

			//Help display
			if (args.Length == 1)
			{
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Help"));
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Help.Normal"));
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Help.Big"));
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Help.Huge"));
				return;
			}

			//Check to see if the BD has a commander and minions
			if (player.ControlledBrain == null)
			{
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Missing.Commander"));
				return;
			}
			bool haveminion = false;
			foreach (AI.Brain.IControlledBrain icb in player.ControlledBrain.Body.ControlledNpcList)
			{
				if (icb != null)
					haveminion = true;
			}
			if (!haveminion)
			{
				DisplayMessage(
					player,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Spacing.Missing.Minion"));
				return;
			}

			switch (args[1].ToLower())
			{
				//Triangle Formation
				case "normal":
					player.ControlledBrain.Body.FormationSpacing = 1;
					break;
				//Line formation
				case "big":
					player.ControlledBrain.Body.FormationSpacing = 2;
					break;
				//Protect formation
				case "huge":
					player.ControlledBrain.Body.FormationSpacing = 3;
					break;
				default:
					DisplayMessage(
						player,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Spacing.UnknownArg",
							args[1]));
					break;
			}
		}
	}
}