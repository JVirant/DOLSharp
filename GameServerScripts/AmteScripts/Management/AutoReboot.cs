using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Events;
using log4net;

namespace DOL.GS
{
    public class AutoReboot
    {
        private static Timer m_timer;
        private static Timer m_timer2;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool AskedReboot;

        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
        	return;
            log.Info("AutoReboot initialized: ");
            m_timer = new Timer(ScanRebootTime, null, 10000, 1000*60*1); // 1 minutes
        }

        [ScriptUnloadedEvent]
        public static void OnScriptsUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            if (m_timer != null)
                m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            if (m_timer2 != null)
                m_timer2.Change(Timeout.Infinite, Timeout.Infinite);
        }


        public static void ScanRebootTime(object state)
        {
            long sec = WorldMgr.GetRegion(51).Time / 1000;
            long min = sec / 60;
            long hours = min / 60;

            // Reboot => 7h si Uptime > 24h et qu'on est mercredi ou dimanche
            log.Info("\t[AMT]\t[Reboot Time Check] (" + DateTime.Now.Hour + "h)");
            if (DateTime.Now.Hour != 7)
                return;
            if (!AskedReboot)
            {
                if (DateTime.Now.DayOfWeek != DayOfWeek.Wednesday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                    return;
                if (hours < 24)
                    return;
            }
            log.Info("\t[AMT]\t[Reboot Time OK] (" + DateTime.Now.Hour + "h)");

            IList<string> textList = new List<string> {"HOST Broadcasts: ", "", "Reboot AUTOMATIQUE dans 5 minutes !!"};
            foreach (GameClient cl in WorldMgr.GetAllPlayingClients())
                cl.Player.Out.SendCustomTextWindow("Broadcast", textList);

            m_timer2 = new Timer(Reboot, null, 60*1000*5, 1000*60*5); // 5 minutes
        }

        public static void Reboot(object state)
        {
            IList<string> textList = new List<string> {"HOST Broadcasts: ", "", "BADABOUM !"};
            foreach (GameClient cl in WorldMgr.GetAllPlayingClients())
            {
                if (cl.Player == null)
                    continue;
                cl.Player.Out.SendCustomTextWindow("Broadcast", textList);
                cl.Player.SaveIntoDatabase();

            }
            GameServer.Instance.Stop();
        }

    }

}