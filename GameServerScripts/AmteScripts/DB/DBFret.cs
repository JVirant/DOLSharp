using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "Fret")]
	public class DBFret : InventoryItem
	{
		private string m_fromPlayer;
		private string m_toPlayer;

        public DBFret()
        {
            
        }

		public DBFret(InventoryItem template, string toPlayerID, string fromPlayerName) : base(template)
		{
			OwnerID = toPlayerID;
			AllowAdd = true;
			ToPlayer = toPlayerID;
			FromPlayer = fromPlayerName;
		}

		[DataElement(AllowDbNull = false)]
		public string FromPlayer
		{
			get
			{
				return m_fromPlayer;
			}
			set
			{
				Dirty = true;
				m_fromPlayer = value;
			}
		}

        [DataElement(AllowDbNull = false, Index = true)]
		public string ToPlayer
		{
			get
			{
				return m_toPlayer;
			}
			set
			{
				Dirty = true;
				m_toPlayer = value;
			}
		}

		public InventoryItem GetInventoryItem()
		{
			InventoryItem tmp = new InventoryItem
				{
					Template = Template,
					ITemplate_Id = ITemplate_Id,
					UTemplate_Id = UTemplate_Id,
					Color = Color,
					Extension = Extension,
					SlotPosition = SlotPosition,
					Count = Count,
					Creator = Creator,
					IsCrafted = IsCrafted,
					SellPrice = SellPrice,
					Condition = Condition,
					Durability = Durability,
					Emblem = Emblem,
					Cooldown = Cooldown,
					Charges = Charges,
					Charges1 = Charges1,
					PoisonCharges = PoisonCharges,
					PoisonMaxCharges = PoisonMaxCharges,
					PoisonSpellID = PoisonSpellID,
					Experience = Experience
				};
			return tmp;
		}
	}
}
