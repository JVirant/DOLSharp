using DOL.GS;
using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "RTInformation")]
	public class RTInformation : DataObject
	{
		public RTInformation(GamePlayer player)
		{
			PlayerName = player.Name;
			X = player.X;
			Y = player.Y;
			Region = player.CurrentRegionID;
		}

		[PrimaryKey(AutoIncrement = true)]
		public long Id { get; set; }

		[DataElement(AllowDbNull = false)]
		public string PlayerName { get; set; }

		[DataElement(AllowDbNull = false)]
		public int X { get; set; }

		[DataElement(AllowDbNull = false)]
		public int Y { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public int Region { get; set; }
	}
}
