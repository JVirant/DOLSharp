using System;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
	[DataTable(TableName="Banque")]
	public class DBBanque : DataObject
	{
        private string m_PlayerID;
        private long m_Money;

		[PrimaryKey]
		public string PlayerID
		{
            get { return m_PlayerID; }
            set { m_PlayerID = value; }
		}

		[DataElement(AllowDbNull=false)]
		public long Money
		{
			get { return m_Money; }
			set { Dirty = true; m_Money = value; }
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBBanque));
		}

		public DBBanque()
		{
			m_PlayerID = null;
			m_Money = 0;
		}
		
		public DBBanque(string playerID)
		{
            m_PlayerID = playerID;
			m_Money = 0;
		}
	}
}
