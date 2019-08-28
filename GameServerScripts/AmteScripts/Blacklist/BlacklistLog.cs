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
	[DataTable(TableName="BlacklistLog")]
	public class BlacklistLog : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(BlacklistLog));
            log.Info("DATABASE BlacklistLog LOADED");
        }

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }

		[DataElement(AllowDbNull = false)]
		public DateTime Date { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string PlayerName { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Group { get; set; }

		[DataElement(AllowDbNull = false)]
		public float Amount { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Reason { get; set; }

	    public BlacklistLog()
        {        
        }

		public BlacklistLog(string player, string group, float amount, string reason)
		{
			Date = DateTime.Now;
			PlayerName = player;
			Group = group;
			Amount = amount;
			Reason = reason;
		}

		public static void Add(GameLiving player, string playerName, float amount, string reason)
		{
			GameServer.Database.AddObject(
				new BlacklistLog(playerName,
				                 player == null || player.Group == null
				                 	? ""
				                 	: player.Group.GetPlayersInTheGroup().Select(p => p.Name).Aggregate((a, b) => a + ";" + b),
				                 amount,
				                 reason));
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