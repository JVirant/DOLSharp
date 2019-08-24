using System;
using DOL.Events;
using DOL.GS;

namespace AmteScripts.Management
{
	public static class StartLocation
	{
		[ScriptLoadedEvent]
		public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
		{
			//We want to be notified whenever a new character is created
			GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new DOLEventHandler(CharacterCreation));
		}

		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new DOLEventHandler(CharacterCreation));
		}

		private static void CharacterCreation(DOLEvent e, object sender, EventArgs arguments)
		{
			CharacterEventArgs charArgs = arguments as CharacterEventArgs;
			if (charArgs == null)
				return;
			charArgs.Character.GuildID = "17118d10-a7e9-4aee-82e5-cd6ca50c0c33";
			charArgs.Character.GuildRank = 8;
			switch (charArgs.Character.Realm)
            {
                case (int)eRealm.Albion:
                default:
                    charArgs.Character.Xpos = 535434;
                    charArgs.Character.Ypos = 547817;
                    charArgs.Character.Zpos = 4800;
                    charArgs.Character.Region = 51;
                    charArgs.Character.BindXpos = 535434;
                    charArgs.Character.BindYpos = 547817;
                    charArgs.Character.BindZpos = 4800;
                    charArgs.Character.BindRegion = 51;
                    charArgs.Character.BindHeading = 2333;
                    break;
                case (int)eRealm.Midgard:
                    charArgs.Character.Xpos = 403686;
                    charArgs.Character.Ypos = 503203;
                    charArgs.Character.Zpos = 4680;
                    charArgs.Character.Region = 51;
                    charArgs.Character.BindXpos = 403686;
                    charArgs.Character.BindYpos = 503203;
                    charArgs.Character.BindZpos = 4680;
                    charArgs.Character.BindRegion = 51;
                    charArgs.Character.BindHeading = 1999;
                    break;
                case (int)eRealm.Hibernia:
                    charArgs.Character.Xpos = 427382;
                    charArgs.Character.Ypos = 416633;
                    charArgs.Character.Zpos = 5712;
                    charArgs.Character.Region = 51;
                    charArgs.Character.BindXpos = 427382;
                    charArgs.Character.BindYpos = 416633;
                    charArgs.Character.BindZpos = 5712;
                    charArgs.Character.BindRegion = 51;
                    charArgs.Character.BindHeading = 2602;
                    break;
            }
		}
	}
}
