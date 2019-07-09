using System;
using DOL.Events;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&invis",
		ePrivLevel.GM,
		"Rend invisible",
		"/invis Rend invisible")]
	public class InvisCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
            client.Player.Stealth(!client.Player.IsStealthed);
			client.Player.TempProperties.setProperty("AMTE_GM_INVIS", client.Player.IsStealthed);
        }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GameObjectEvent.AddToWorld, (ev, s, a) =>
			                                                    {
			                                                    	var p = s as GamePlayer;
																	if (p != null && p.TempProperties.getProperty("AMTE_GM_INVIS", false))
																		p.Stealth(true);
			                                                    });
		}
	}
}