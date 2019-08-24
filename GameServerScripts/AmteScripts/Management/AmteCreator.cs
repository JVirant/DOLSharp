using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Database;
using DOL.Events;
using log4net;

namespace DOL.GS.Scripts
{
    public static class AmteCreator
    {
        #region Starting
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Any, 42421);
            _listener.Start();
            _worker = new Thread(_Work);
            _worker.Start();
            if (log.IsInfoEnabled)
                log.Info("AmteCreator initialized...");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptsUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            _running = false;
            try { _listener.Stop(); } catch {}
        }
        #endregion

        private static TcpListener _listener;
        private static Thread _worker;
        private static bool _running;

        private static void _Work()
        {
            while (_running)
            {
                try
                {
                    var cl = _listener.AcceptTcpClient();
                    var buffer = new byte[2048];
                    cl.GetStream().BeginRead(buffer, 0, buffer.Length, _ReadingCallback,
                                             new KeyValuePair<TcpClient, byte[]>(cl, buffer));
                }
                catch (Exception e)
                {
                    log.Error("[AmteCreator]", e);
                }
            }
        }

        private static void _ReadingCallback(IAsyncResult ar)
        {
            KeyValuePair<TcpClient, byte[]> kvp;
            try
            {
                kvp = (KeyValuePair<TcpClient, byte[]>)ar.AsyncState;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("[AmteCreator]", e);
                return;
            }
            try
            {
                if (kvp.Key.GetStream().EndRead(ar) >= 4)
                {
                    var length = (kvp.Value[1] << 8) + kvp.Value[2];
                    var arg = Encoding.UTF8.GetString(kvp.Value, 3, length);
                    switch (kvp.Value[0])
                    {
                        case 0:
                            _ReloadItem(arg);
                            break;
                        case 1:
                            _ReloadLootMob(arg);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("[AmteCreator]", e);
            }
            try
            {
                kvp.Key.Close();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("[AmteCreator] Close client", e);
            }
        }

        private static void _ReloadItem(string id)
        {
            GameServer.Database.UpdateInCache<ItemTemplate>(id);
        }

        private static void _ReloadLootMob(string mobName)
        {
            LootMgr.RefreshGenerators(WorldMgr.GetNPCsByName(mobName, eRealm.None).FirstOrDefault());
        }
    }
}
