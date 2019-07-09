using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using log4net;


namespace DOL.GS.GameEvents
{
    public static class DeathLog
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));

            log.Info("DeathLog initialized");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));
        }


        public static void LivingKillEnnemy(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.AddObject(new DBDeathLog((GameObject)sender, ((DyingEventArgs)args).Killer));
        }
    }

}
