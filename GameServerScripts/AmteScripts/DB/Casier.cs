using System;
using DOL.GS;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using System.Linq;
using log4net;

namespace DOL.Database
{
	/// <summary>
	/// Prison
	/// </summary>
	[DataTable(TableName="Casier")]
	public class Casier : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(Casier));
            log.Info("DATABASE Casier LOADED");
        }

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public DateTime Date { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string Author { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string AccountName { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public bool StaffOnly { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Reason { get; set; }

		public Casier()
	    {
	    }

		public Casier(string author, string account, string reason, bool staffOnly)
		{
			Author = author;
			Date = DateTime.Now;
			AccountName = account;
			Reason = reason;
			StaffOnly = staffOnly;
		}

		public void Save()
		{
			Dirty = true;
			if (!IsPersisted)
				GameServer.Database.AddObject(this);
			else
				GameServer.Database.SaveObject(this);
		}
	}
}