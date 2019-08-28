using System;
using DOL.GS;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using log4net;

namespace DOL.Database
{
    /// <summary>
    /// Texte des pnj
    /// </summary>
    [DataTable(TableName = "TeleportNPC")]
    public class DBTeleportNPC : DataObject
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(DBTeleportNPC));
            log.Info("DATABASE DBTeleportNPC LOADED");
        }

        private string m_mobID;
        private string m_Text;
        private string m_Text_Refuse;
        private string m_JumpPos;
        private int m_Range;
        private byte m_Level;

        [DataElement(AllowDbNull = false)]
        public string MobID
        {
            get { return m_mobID; }
            set
            {
                Dirty = true;
                m_mobID = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get { return m_Text; }
            set
            {
                Dirty = true;
                m_Text = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string Text_Refuse
        {
            get { return m_Text_Refuse; }
            set
            {
                Dirty = true;
                m_Text_Refuse = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string JumpPosition
        {
            get { return m_JumpPos; }
            set
            {
                Dirty = true;
                m_JumpPos = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public byte Level
        {
            get { return m_Level; }
            set
            {
                Dirty = true;
                m_Level = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Range
        {
            get { return m_Range; }
            set
            {
                Dirty = true;
                m_Range = value;
            }
        }
    }
}