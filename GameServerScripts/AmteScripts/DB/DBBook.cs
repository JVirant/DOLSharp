using System;
using DOL.GS;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using log4net;

namespace DOL.Database
{
	/// <summary>
	/// DBBook
	/// </summary>
	[DataTable(TableName="Book")]
	public class DBBook : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(DBBook));
            log.Info("DATABASE Book LOADED");
        }

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string PlayerID { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string Author { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string Name { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string Title { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Ink { get; set; }

		[DataElement(AllowDbNull = false)]
		public string InkId { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Text { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public bool IsInLibrary { get; set; }

		public DBBook()
	    {
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