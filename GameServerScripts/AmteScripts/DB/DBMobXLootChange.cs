using System;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
	[DataTable(TableName="MobXLootChanger")]
	public class DBMobXLootChanger : DataObject
	{
		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBMobXLootChanger));
		}

		protected string	m_MobName = "";
		protected string	m_LootChangerTemplateName = "";
		protected int		m_dropCount;
		
		public DBMobXLootChanger()
		{
		}
		
		[DataElement(AllowDbNull=false)]
		public string MobName
		{
			get {return m_MobName;}
			set	
			{
				Dirty = true;
				m_MobName = value;
			}
		}
		
		[DataElement(AllowDbNull=false)]
        public string LootChangerTemplateName
		{
            get { return m_LootChangerTemplateName; }
			set	
			{
				Dirty = true;
                m_LootChangerTemplateName = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int DropCount
		{
			get {return m_dropCount;}
			set	
			{
				Dirty = true;
				m_dropCount = value;
			}
		}
	}
}
