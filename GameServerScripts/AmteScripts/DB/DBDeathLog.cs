using System;
using System.Reflection;
using DOL.Events;
using log4net;
using DOL.GS;
using DOL.Database.Attributes;

namespace DOL.Database
{
        [DataTable(TableName = "DeathLog")]
        public class DBDeathLog : DataObject
        {
            private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            public DBDeathLog(GameObject killed, GameObject killer)
            {
				Killer = "(null)";
				KillerClass = "(null)";
				if (killer != null)
                {
                    Killer = killer.Name;
                    KillerClass = killer.GetType().ToString();
                }
                Killed = killed.Name;
                KilledClass = killed.GetType().ToString();
                X = killed.X;
                Y = killed.Y;
                Region = killed.CurrentRegionID;
                DeathDate = DateTime.Now;
            }

            [PrimaryKey(AutoIncrement = true)]
            public long Id { get; set; }

            [DataElement(AllowDbNull = false)]
            public String Killer { get; set; }

            [DataElement(AllowDbNull = false)]
            public String KillerClass { get; set; }

            [DataElement(AllowDbNull = false)]
            public String Killed { get; set; }

            [DataElement(AllowDbNull = false)]
            public String KilledClass { get; set; }

            [DataElement(AllowDbNull = false, Index = true)]
            public int X { get; set; }

            [DataElement(AllowDbNull = false, Index = true)]
            public int Y { get; set; }

            [DataElement(AllowDbNull = false, Index = true)]
            public int Region { get; set; }

            [DataElement(AllowDbNull = false, Index = true)]
            public DateTime DeathDate { get; set; }

            #region Init
            private static bool Loaded = false;

            [ScriptLoadedEvent]
            public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
            {
                if (Loaded)
                    return;
                GameServer.Database.RegisterDataObject(typeof(DBDeathLog));
                Loaded = true;
                log.Info("DATABASE DBDeathLog LOADED");
            }
            #endregion
        }
}
