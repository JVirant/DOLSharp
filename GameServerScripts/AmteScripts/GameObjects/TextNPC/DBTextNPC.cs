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
	[DataTable(TableName="TextNPC")]
	public class DBTextNPC : DataObject
	{
		private static bool Loaded = false;
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			Init();
		}

		public static void Init()
		{
			if(Loaded)
				return;
			GameServer.Database.RegisterDataObject(typeof(DBTextNPC));
			Loaded = true;
			log.Info("DATABASE DBTextNPC LOADED");
		}

		private	string	m_mobID;
		private string	m_mobName;
		private byte 	m_mobRealm;
		private string	m_Text;
		private	string	m_Reponse;
		private string	m_ReponseSpell;
		private string	m_ReponseEmote;
		private string	m_RandomPhraseEmote;
		private int		m_PhraseInterval;
		private string	m_Condition;

		[DataElement(AllowDbNull=false)]
		public string MobID
		{
			get { return m_mobID; }
			set 
			{ 
				Dirty = true;
				m_mobID = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public string MobName
		{
			get { return m_mobName; }
			set 
			{ 
				Dirty = true;
				m_mobName = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public byte MobRealm
		{
			get { return m_mobRealm; }
			set	
			{
				Dirty = true;
				m_mobRealm = value; 
			}
		}

		[DataElement(AllowDbNull=false)]
		public string Text
		{
			get { return m_Text; }
			set 
			{ 
				Dirty = true;
				m_Text = value; 
			}
		}

		[DataElement(AllowDbNull=true)]
		public string Reponse
		{
			get { return m_Reponse; }
			set 
			{ 
				Dirty = true;
				m_Reponse = value; 
			}
		}

		[DataElement(AllowDbNull=true)]
		public string ReponseSpell
		{
			get { return m_ReponseSpell; }
			set 
			{ 
				Dirty = true;
				m_ReponseSpell = value;
			}
		}

		[DataElement(AllowDbNull=true)]
		public string ReponseEmote
		{
			get { return m_ReponseEmote; }
			set 
			{ 
				Dirty = true;
				m_ReponseEmote = value;
			}
		}

		[DataElement(AllowDbNull=true)]
		public string RandomPhraseEmote
		{
			get { return m_RandomPhraseEmote; }
			set 
			{ 
				Dirty = true;
				m_RandomPhraseEmote = value; 
			}
		}

		[DataElement(AllowDbNull=true)]
		public int PhraseInterval
		{
			get { return m_PhraseInterval; }
			set 
			{ 
				Dirty = true;
				m_PhraseInterval = value; 
			}
		}

		[DataElement(AllowDbNull=true)]
		public string Condition
		{
			get { return m_Condition; }
			set 
			{ 
				Dirty = true;
				m_Condition = value; 
			}
		}
	}
}