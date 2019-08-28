using System;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
    [DataTable(TableName = "AmteBrainsParam")]
    public class DBBrainsParam : DataObject
    {
        private string m_mobID; //ID du mob
        private string m_Param; //Param
        private string m_Value; //Value

        [DataElement(AllowDbNull = false)]
        public string MobID
        {
            get { return m_mobID; }
            set { Dirty = true; m_mobID = value; }
        }

        [DataElement(AllowDbNull = false)]
        public string Param
        {
            get { return m_Param; }
            set { Dirty = true; m_Param = value; }
        }

        [DataElement(AllowDbNull = false)]
        public string Value
        {
            get { return m_Value; }
            set { Dirty = true; m_Value = value; }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(DBBrainsParam));
        }
    }
}
