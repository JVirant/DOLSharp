using System;
using DOL.GS;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using log4net;

namespace DOL.Database
{
	/// <summary>
	/// Prison
	/// </summary>
	[DataTable(TableName="Prisoner")]
	public class Prisoner : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[GameServerStartedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof (Prisoner));
            log.Info("DATABASE Prisoner LOADED");
        }

		[PrimaryKey]
		public string PlayerId { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Name { get; set; }

		[DataElement(AllowDbNull=false)]
		public int Cost { get; set; }

		[DataElement(AllowDbNull=false)]
		public DateTime Sortie { get; set; }

		[DataElement(AllowDbNull=false)]
		public bool RP { get; set; }

		[DataElement(AllowDbNull=false)]
		public string Raison { get; set; }

        public Prisoner()
        {
            
        }

		public Prisoner(GameObject player)
		{
			PlayerId = player.InternalID;
			Name = player.Name;
			Sortie = DateTime.MinValue;
		}

		public Prisoner(DOLCharacters player)
		{
			PlayerId = player.ObjectId;
			Name = player.Name;
			Sortie = DateTime.MinValue;
		}
	}
}