using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
    public class AnimMob : AmteMob
    {
        private RegionTimer mtimer;

    	protected ushort _SpellID;

        public int Rplay(RegionTimer callingTimer)
        {
            //Si un joueur est présent dans un radius de 2k le mob se met en mouvement
            foreach (GamePlayer pl1 in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
                pl1.Out.SendSpellCastAnimation(this, _SpellID, 1111);
            return 5000;
        }

        public override bool AddToWorld()
        {
        	SetOwnBrain(new BlankBrain());
            if (!base.AddToWorld()) return false;
            //On démarre 
            mtimer = new RegionTimer(this, Rplay);
            mtimer.Start(3000);
            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (!base.RemoveFromWorld()) return false;
            mtimer.Stop();
            return true;
        }

        #region IAmteNPC
		public override AmteCustomParam GetCustomParam()
		{
			var cp = base.GetCustomParam();
			cp.next = new AmteCustomParam("SpellID", () => _SpellID.ToString(), v => _SpellID = ushort.Parse(v), "0");
			return cp;
		}
        #endregion
    }

    public class DjipDrums : AnimMob { public DjipDrums() { _SpellID = 1125; } }
    public class DjipLuth : AnimMob { public DjipLuth() { _SpellID = 1111; } }
    public class DjipFlute : AnimMob { public DjipFlute() { _SpellID = 5175; } }
}
