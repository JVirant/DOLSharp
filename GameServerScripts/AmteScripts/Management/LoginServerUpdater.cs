using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.GS;
using DOL.Events;
using log4net;

namespace DOL.Scripts
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class LoginServerUpdater
    {
        public const string HOST = "loginserver.amtenael.com";
        public const int PORT = 10900;
        public const int INTERVAL = 60; //En secondes
        public static string LOGIN = "Amtenaël Serveur";
        public static string PASS = "passamte854";

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Initialisation
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (AmteUtils.IsLiveServer)
            {
                LOGIN = "AMTENAEL";
                PASS = "passamte854";
                Start();
            }
            else if (AmteUtils.IsTestServer)
            {
                LOGIN = "AMTEST";
                PASS = "TESTpassP";
                //Start();
            }
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            if (AmteUtils.IsLiveServer || AmteUtils.IsTestServer)
                Stop();
        }

        private static Timer UpdateTimer;
        public static void Start()
        {
            try
            {
                m_processCpuUsedCounter = new PerformanceCounter("Process", "% processor time", GetProcessCounterName());
                m_processCpuUsedCounter.NextValue();
            }
            catch (Exception ex)
            {
                m_processCpuUsedCounter = null;
                if (log.IsWarnEnabled)
                    log.Warn(ex.GetType().Name + " ProcessCpuUsedCounter won't be available: " + ex.Message);
            }

            UpdateTimer = new Timer(Update, null, 0, INTERVAL * 1000);
        }

        public static void Stop()
        {
        	try
        	{
				UpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
				UpdateTimer = null;
        	}
        	catch (Exception e)
        	{
        		log.Error("Stop LoginServerUpdater", e);
        	}
        }

        /// <summary>
        /// Find the process counter name
        /// </summary>
        /// <returns></returns>
        public static string GetProcessCounterName()
        {
            Process process = Process.GetCurrentProcess();
            int id = process.Id;
            PerformanceCounterCategory perfCounterCat = new PerformanceCounterCategory("Process");
            foreach (DictionaryEntry entry in perfCounterCat.ReadCategory()["id process"])
            {
                string processCounterName = (string)entry.Key;
                if (((InstanceData)entry.Value).RawValue == id)
                    return processCounterName;
            }
            return "";
        }
        #endregion

        private static long m_lastBytesOut;
        private static long m_lastBytesIn;

        private static PerformanceCounter m_processCpuUsedCounter;
        private static void Update(object obj)
        {
            try
            {
            	MemoryStream MS = new MemoryStream();

                //Packet ID
                MS.WriteByte(51);

                //Login 
                MS.WriteByte((byte)(LOGIN.Length & 0xff));
                MS.WriteByte((byte)(LOGIN.Length >> 8));
                byte[] bytes = Encoding.Default.GetBytes(LOGIN);
                MS.Write(bytes, 0, bytes.Length);

                //Pass
                MS.WriteByte((byte)(PASS.Length & 0xff));
                MS.WriteByte((byte)(PASS.Length >> 8));
                bytes = Encoding.Default.GetBytes(PASS);
                MS.Write(bytes, 0, bytes.Length);

                //Clients (octect) - SHORT
                ushort NB_Clients = (ushort)WorldMgr.GetAllClients().Count;
                MS.WriteByte((byte)(NB_Clients & 0xff));
                MS.WriteByte((byte)(NB_Clients >> 8));

                //Upload (octect) - INT
                uint outRate = (uint)((Statistics.BytesOut - m_lastBytesOut) / INTERVAL);
                m_lastBytesOut = Statistics.BytesOut;
                MS.WriteByte((byte)(outRate & 0x000000ff));
                MS.WriteByte((byte)((outRate & 0x0000ff00) >> 8));
                MS.WriteByte((byte)((outRate & 0x00ff0000) >> 16));
                MS.WriteByte((byte)((outRate & 0xff000000) >> 24));
                //Download (octect) - INT
                uint inRate = (uint)((Statistics.BytesIn - m_lastBytesIn) / INTERVAL);
                m_lastBytesIn = Statistics.BytesIn;
                MS.WriteByte((byte)(inRate & 0x000000ff));
                MS.WriteByte((byte)((inRate & 0x0000ff00) >> 8));
                MS.WriteByte((byte)((inRate & 0x00ff0000) >> 16));
                MS.WriteByte((byte)((inRate & 0xff000000) >> 24));

                //CPU - BYTE
                byte CPU = 0x00;
                if (m_processCpuUsedCounter != null)
                    CPU = (byte)m_processCpuUsedCounter.NextValue();
                MS.WriteByte(CPU);

                //Memoire (octect) - INT
                uint Memory = (uint)GC.GetTotalMemory(false);
                MS.WriteByte((byte)(Memory & 0x000000ff));
                MS.WriteByte((byte)((Memory & 0x0000ff00) >> 8));
                MS.WriteByte((byte)((Memory & 0x00ff0000) >> 16));
                MS.WriteByte((byte)((Memory & 0xff000000) >> 24));

                log.Debug("Cl=" + NB_Clients + " Up=" + outRate + " Down=" + inRate + " CPU=" + CPU + " Mem=" + Memory);


                //Connexion au serveur
                TcpClient Client = new TcpClient();
                Client.Connect(HOST, PORT);
                NetworkStream NS = Client.GetStream();
                //Envoi du packet
            	byte[] packet = MS.GetBuffer();
                NS.Write(packet, 0, (int)MS.Length);

                //Reception du packet de confirmation
                bytes = new byte[256];
                NS.Read(bytes, 0, bytes.Length);

                //Lecture du packet
                MS = new MemoryStream(bytes, false);
                int code = MS.ReadByte();
                if (code != 52)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("LoginServer: Mauvais login/pass, Packet ID inconnu: " + code);
                }
//                else
//                    log.Warn("LoginServer: updated");

                NS.Close();
                Client.Close();
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("LoginServer: Erreur lors de la connexion:\r\n", e);
            }
        }
    }
}

