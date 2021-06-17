using System;
using DOL.Events;
using DOL.GS.Quests;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&gmquest",
		ePrivLevel.GM,
		"Quests management",
		"'/gmquest reload' Refresh all quests from database")]
	public class GMQuestCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			client.Out.SendCustomTextWindow("Quest reload status", DataQuestJsonMgr.ReloadQuests());
		}
	}
}
