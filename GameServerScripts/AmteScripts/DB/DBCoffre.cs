/* Maj des tables:
ALTER TABLE `coffre` ADD `KeyItem` TEXT NOT NULL ,
ADD `LockDifficult` INT( 11 ) NOT NULL ;
*/

using System;
using DOL.GS;
using DOL.GS.Scripts;
using DOL.Events;
using DOL.Database.Attributes;
using System.Reflection;
using log4net;

namespace DOL.Database
{
	/// <summary>
	/// Coffre
	/// </summary>
	[DataTable(TableName="Coffre")]
	public class DBCoffre : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBCoffre));
			log.Info("DATABASE Coffre LOADED");
        }

        [GameServerStartedEvent]
        public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
        {
			int i = 0;
			foreach (var obj in GameServer.Database.SelectAllObjects<DBCoffre>())
			{
				i++;
				GameCoffre coffre = new GameCoffre();
				coffre.LoadFromDatabase(obj);
				coffre.AddToWorld();
			}
			log.Info(i + " GameCoffre loaded in world.");
		}


		private string		m_name;
		private int			m_x;
		private int			m_y;
		private int			m_z;
		private ushort		m_heading;
		private ushort		m_region;
		private ushort		m_model;
		private int			m_itemInterval;
		private DateTime	m_lastOpen;
		private string		m_itemList;
		private int			m_itemChance;
		private int			m_lockDifficult;
		private string		m_keyItem;

		[DataElement(AllowDbNull=false)]
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				Dirty = true;
				m_name = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int X
		{
			get
			{
				return m_x;
			}
			set
			{   
				Dirty = true;
				m_x = value;
			}
		}
		
		[DataElement(AllowDbNull=false)]
		public int Y
		{
			get
			{
				return m_y;
			}
			set
			{   
				Dirty = true;
				m_y = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int Z
		{
			get
			{
				return m_z;
			}
			set
			{   
				Dirty = true;
				m_z = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public ushort Heading
		{
			get
			{
				return m_heading;
			}
			set
			{   
				Dirty = true;
				m_heading = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public ushort Region
		{
			get
			{
				return m_region;
			}
			set
			{   
				Dirty = true;
				m_region = value;
			}
		}
		
		[DataElement(AllowDbNull=false)]
		public ushort Model
		{
			get
			{
				return m_model;
			}
			set
			{   
				Dirty = true;
				m_model = value;
			}
		}
		
		/// <summary>
		/// Temps en minutes avant la réapparition d'un item
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int ItemInterval
		{
			get 
			{ 
				return m_itemInterval;
			}
			set 
			{
				Dirty = true;
				m_itemInterval = value;
			}
		}

		/// <summary>
		/// Pourcentage de chance d'avoir un item
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int ItemChance
		{
			get 
			{ 
				return m_itemChance;
			}
			set 
			{
				Dirty = true;
				m_itemChance = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public DateTime LastOpen
		{
			get 
			{ 
				return m_lastOpen;
			}
			set 
			{
				Dirty = true;
				m_lastOpen = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public string ItemList
		{
			get
			{
				return m_itemList;
			}
			set
			{
				Dirty = true;
				m_itemList = value;
			}
		}

		/// <summary>
		/// Difficulté d'ouvrir la serrure avec un crochet (sur 1000)
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int LockDifficult
		{
			get
			{
				return m_lockDifficult;
			}
			set
			{
				Dirty = true;
				m_lockDifficult = value;
			}
		}

		/// <summary>
		/// Id_nb de la clef pour ouvrir le coffre
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string KeyItem
		{
			get
			{
				return m_keyItem;
			}
			set
			{
				Dirty = true;
				m_keyItem = value;
			}
		}
	}
}
