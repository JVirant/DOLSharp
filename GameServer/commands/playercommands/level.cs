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
using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&level", //command to handle
		ePrivLevel.Player, //minimum privelege level
		"Commands.Players.Level.Description",
		"Commands.Players.Level.Usage")] //usage
	public class LevelCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (ServerProperties.Properties.SLASH_LEVEL_TARGET <= 1)
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Level.Disabled"));
				return;
			}

			if (client.Player.TargetObject is GameTrainer == false)
			{
				client.Player.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Level.NeedTrainer"),
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (!ServerProperties.Properties.ALLOW_CATA_SLASH_LEVEL)
			{
				switch ((eCharacterClass)client.Player.CharacterClass.ID)
				{
					case eCharacterClass.Heretic:
					case eCharacterClass.Valkyrie:
					case eCharacterClass.Warlock:
					case eCharacterClass.Vampiir:
					case eCharacterClass.Bainshee:
					case eCharacterClass.MaulerAlb:
					case eCharacterClass.MaulerHib:
					case eCharacterClass.MaulerMid:
						{
							client.Player.Out.SendMessage(
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Level.Disabled.Class"),
								eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
				}
			}
			if (!client.Player.CanUseSlashLevel)
			{
				client.Player.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Level.CantUseSlashLevel",
						ServerProperties.Properties.SLASH_LEVEL_REQUIREMENT),
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			//if there is a level 50.. calculate the xp needed to get to
			//level x from the current level and give it to the player

			// only do this if the players level is  < target level
			if (client.Player.Experience >= client.Player.GetExperienceNeededForLevel(ServerProperties.Properties.SLASH_LEVEL_TARGET - 1))
			{
				client.Player.Out.SendMessage(
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Level.LevelUpTo",
						ServerProperties.Properties.SLASH_LEVEL_TARGET),
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			int targetLevel = ServerProperties.Properties.SLASH_LEVEL_TARGET;

			if( targetLevel < 1 || targetLevel > 50 )
				targetLevel = 20;

			long newXP;
			newXP = client.Player.GetExperienceNeededForLevel(targetLevel - 1) - client.Player.Experience;

			if (newXP < 0)
				newXP = 0;

			client.Player.GainExperience(GameLiving.eXPSource.Other, newXP);
			client.Player.UsedLevelCommand = true;
			client.Player.Out.SendMessage(
				LanguageMgr.GetTranslation(
					client.Account.Language,
					"Commands.Players.Level.Rewarded",
					ServerProperties.Properties.SLASH_LEVEL_TARGET),
				eChatType.CT_System, eChatLoc.CL_SystemWindow);
			client.Player.SaveIntoDatabase();
		}
	}
}
