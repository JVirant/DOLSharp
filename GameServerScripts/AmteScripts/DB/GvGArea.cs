using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Texte des pnj
	/// </summary>
	[DataTable(TableName = "GvGArea")]
	public class DBGvGArea : DataObject
	{
		[PrimaryKey(AutoIncrement = true)]
		public int ID { get; set; }

		[DataElement(AllowDbNull = false)]
		public DateTime LastClaim { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string LordID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Type { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string Name { get; set; }

		[DataElement(AllowDbNull = false, Index = true)]
		public string GuildID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string GuardTemplates { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Allied { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Enemies { get; set; }

		[DataElement(AllowDbNull = false)]
		public string Settings { get; set; }

		[DataElement(AllowDbNull = false)]
		public int X { get; set; }

		[DataElement(AllowDbNull = false)]
		public int Y { get; set; }

		[DataElement(AllowDbNull = false)]
		public ushort Region;

		[DataElement(AllowDbNull = false)]
		public ushort Radius { get; set; }

		public DBGvGArea()
		{
			Allied = "";
			Enemies = "";
			GuardTemplates = "";
			GuildID = "";
			LastClaim = DateTime.MinValue;
			LordID = "";
			Name = "";
			Radius = 0;
			Region = 0;
			Settings = "";
			Type = "";
			X = 0;
			Y = 0;
		}
	}
}