using System.Collections;
using DOL.AI.Brain;
using DOL.Database;
using System.Collections.Generic;
using DOL.GS.Utils;

namespace DOL.GS.Scripts
{
	public class NightMob : AmteMob
	{
		public int StartHour = 0;
		public int EndHour = 24;

		private RegionTimer timer;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            if (timer == null)
            {
                timer = new RegionTimer(this, ScanHour);
                timer.Start(1000);
            }
            return true;
        }

		private int ScanHour(RegionTimer callingTimer)
		{
			int hour = (int)(WorldMgr.GetCurrentGameTime() / 1000 / 60 / 54);
			bool add = false;

			if (StartHour < EndHour)
			{
				if (StartHour <= hour && hour < EndHour)
					add = true;
			}
			else if (StartHour > EndHour)
			{
				if (StartHour >= hour && hour < EndHour)
					add = true;
			}
			else if (StartHour == hour)
				add = true;

            if (add && IsAlive && ObjectState != eObjectState.Active)
            {
                X = SpawnPoint.X;
                Y = SpawnPoint.Y;
                Z = SpawnPoint.Z;
                Heading = SpawnHeading;
                AddToWorld();
            }
            else if (!add && IsAlive && ObjectState == eObjectState.Active)
                Delete();

			return 60000;
		}

		public override void DeleteFromDatabase()
		{
			base.DeleteFromDatabase();
			if (timer != null)
				timer.Stop();
		}

		public override AmteCustomParam GetCustomParam()
		{
			var cp = base.GetCustomParam();
			cp.next = new AmteCustomParam("StartHour", () => StartHour.ToString(), v => StartHour = int.Parse(v))
			          {
						  next = new AmteCustomParam("EndHour", () => EndHour.ToString(), v => EndHour = int.Parse(v))
			          };
			return cp;
		}
	}
}
