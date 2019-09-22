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
using DOL.GS.Quests;
using System;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&quest",
		new string[] {"&quests"},
		ePrivLevel.Player,
		"Commands.Players.Quest.Description",
		"Commands.Players.Quest.Usage")]
	public class QuestCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "quest"))
				return;

			string message = "\n";
			if (client.Player.QuestList.Count == 0)
				message += LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.NoPending") + "\n";
			else
			{
				message += LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.WorkingOn") + "\n";
				foreach (AbstractQuest quest in client.Player.QuestList)
				{
					message += LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.OnStep", quest.Step, quest.Name) + "\n";
					message += LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.WhatTodo", quest.Description);
				}
			}
			if (client.Player.QuestListFinished.Count == 0)
				message += "\n" + LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.NotComplete") + "\n";
			else
			{
				message += "\n"+ LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.Completed") +"\n";

				// Need to protect from too long a list.  
				// We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
				foreach (AbstractQuest quest in client.Player.QuestListFinished)
				{
					message += quest.Name + LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Quest.QuestCompletedk") +"\n";

					if (message.Length > 1500)
					{
						DisplayMessage(client, message);
						message = "";
					}
				}
			}
			DisplayMessage(client, message);
		}
	}
}