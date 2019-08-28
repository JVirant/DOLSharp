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
	[DataTable(TableName="RvrPlayer")]
	public class RvrPlayer : DataObject
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(RvrPlayer));
            log.Info("DATABASE RvrPlayer LOADED");
        }

		[PrimaryKey]
		public string PlayerID { get; set; }

		[DataElement(AllowDbNull = false)]
		public string GuildID { get; set; }

        [DataElement(AllowDbNull = false)]
        public int GuildRank { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldX { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldY { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldZ { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldHeading { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldRegion { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldBindX { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldBindY { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldBindZ { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldBindHeading { get; set; }

        [DataElement(AllowDbNull = false)]
        public int OldBindRegion { get; set; }

	    public RvrPlayer()
        {
            
        }

		public RvrPlayer(GamePlayer player)
		{
		    PlayerID = player.InternalID;
		    GuildID = player.GuildID;
		    GuildRank = player.GuildRank != null ? player.GuildRank.RankLevel : 9;

		    OldX = player.X;
		    OldY = player.Y;
            OldZ = player.Z;
            OldHeading = player.Heading;
		    OldRegion = player.CurrentRegionID;

		    OldBindX = player.BindXpos;
            OldBindY = player.BindYpos;
            OldBindZ = player.BindZpos;
            OldBindHeading = player.BindHeading;
            OldBindRegion = player.BindRegion;
		}

        public void	ResetCharacter(GamePlayer player)
        {
            if (player.ObjectId != PlayerID)
                return;
            player.GuildID = GuildID;
            player.GuildRank = player.Guild != null ? player.Guild.GetRankByID(GuildRank) : null;

            player.BindXpos = OldBindX;
            player.BindYpos = OldBindY;
            player.BindZpos = OldBindZ;
            player.BindHeading = OldBindHeading;
            player.BindRegion = OldBindRegion;
        }

        public void ResetCharacter(DOLCharacters ch)
        {
            if (ch.ObjectId != PlayerID)
                return;
            ch.GuildID = GuildID;
            ch.GuildRank = (ushort)GuildRank;

            ch.Xpos = OldX;
            ch.Ypos = OldY;
            ch.Zpos = OldZ;
            ch.Region = OldRegion;

            ch.BindXpos = OldBindX;
            ch.BindYpos = OldBindY;
            ch.BindZpos = OldBindZ;
            ch.BindHeading = OldBindHeading;
            ch.BindRegion = OldBindRegion;
        }
    }
}