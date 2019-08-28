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
	[DataTable(TableName="Blacklist")]
	public class BlacklistPlayer : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(BlacklistPlayer));
            log.Info("DATABASE Blacklist LOADED");
        }

		[PrimaryKey]
		public string PlayerID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string PlayerName { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public float Reputation { get; set; }

		[DataElement(AllowDbNull = false)]
		public int HasReported { get; set; }

		[DataElement(AllowDbNull = false)]
		public int KilledBlacklisted { get; set; }

		[DataElement(AllowDbNull = false)]
		public int BeReported { get; set; }

	    public BlacklistPlayer()
        {
			Reputation = 0.0f;
			HasReported = 0;
			KilledBlacklisted = 0;
			BeReported = 0;            
        }

		public BlacklistPlayer(GamePlayer player)
		{
			PlayerID = player.InternalID;
			PlayerName = player.Name;
			Reputation = 0.0f;
			HasReported = 0;
			KilledBlacklisted = 0;
			BeReported = 0;
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