//Area Effect DB:
//
// Effect - int
// Interval - int
// Heal/Harm - int
// Rayon - int
//
// Optional:
// X
// Y
// Z
// RegionID



using System;
using DOL.GS;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using log4net;

namespace DOL.Database
{
	/// <summary>
	/// Texte des pnj
	/// </summary>
	[DataTable(TableName="AreaEffect")]
	public class DBAreaEffect : DataObject
	{
		private int m_Effect;
		private int m_IntervalMin;
		private int m_IntervalMax;
		private int m_HealHarm;
        private int m_Mana;
        private int m_Endurance;
		private int m_Radius;
		private int m_MissChance;
		private string m_Message;

		#region Init
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static bool Loaded = false;

	    [ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			Init();
		}

		public static void Init()
		{
			if(Loaded)
				return;
			GameServer.Database.RegisterDataObject(typeof(DBAreaEffect));
			Loaded = true;
			log.Info("DATABASE DBAreaEffect LOADED");
		}
		#endregion

		[DataElement(AllowDbNull=false)]
		public string MobID { get; set; }

		[DataElement(AllowDbNull=false)]
		public int Effect
		{
			get { return m_Effect; }
			set 
			{ 
				Dirty = true;
				m_Effect = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public int IntervalMin
		{
			get { return m_IntervalMin; }
			set 
			{ 
				Dirty = true;
				m_IntervalMin = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public int IntervalMax
		{
			get { return m_IntervalMax; }
			set 
			{ 
				Dirty = true;
				m_IntervalMax = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public int HealHarm
		{
			get { return m_HealHarm; }
			set 
			{ 
				Dirty = true;
				m_HealHarm = value; 
			}
		}

        [DataElement(AllowDbNull = false)]
        public int Mana
        {
            get { return m_Mana; }
            set
            {
                Dirty = true;
                m_Mana = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Endurance
        {
            get { return m_Endurance; }
            set
            {
                Dirty = true;
                m_Endurance = value;
            }
        }

		[DataElement(AllowDbNull=false)]
		public int MissChance
		{
			get { return m_MissChance; }
			set 
			{ 
				Dirty = true;
				m_MissChance = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public int Radius
		{
			get { return m_Radius; }
			set 
			{ 
				Dirty = true;
				m_Radius = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public string Message
		{
			get { return m_Message; }
			set 
			{
				Dirty = true;
				m_Message = value;
			}
		}
	}
}