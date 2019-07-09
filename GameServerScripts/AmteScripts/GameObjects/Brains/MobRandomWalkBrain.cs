using System;
using DOL.Events;

namespace DOL.AI.Brain
{
    public class RandomWalkBrain : AmteMobBrain
	{
        public int SpeedMin = 30;
        public int SpeedMax = 120;
        public virtual int MissRate
        {
            get { return 25; }
        }

        private bool m_CanWalk = true;
		private bool m_walk;

        public bool Walk
        {
            get { return m_CanWalk; }
            set
            {
                m_CanWalk = value;
                if (value && m_walk && !Body.IsMoving)
                    m_walk = false;
            }
        }

        public override bool CanRandomWalk
        {
            get
            {
                return (!Body.InCombat && Walk && !m_walk && m_aggroTable.Count == 0);
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
            
            if(e == GameNPCEvent.ArriveAtTarget || e == GameLivingEvent.Dying)
                m_walk = false;
            else if(e == GameNPCEvent.WalkTo)
                m_walk = true;
        }
	}
}
