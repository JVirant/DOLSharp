using DOL.Events;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
	public class TeleportNPCBrain : AmteMobBrain
	{
		public override int ThinkInterval
		{
			get { return 500; }
		}

		public override void Think()
		{
			base.Think();
            if (Body is TeleportNPC)
                ((TeleportNPC)Body).JumpArea();
		}

        protected override int BrainTimerCallback(RegionTimer callingTimer)
        {
            if (!m_body.IsAlive || m_body.ObjectState != GameObject.eObjectState.Active)
            {
                //Stop the brain for dead or inactive bodies
                Stop();
                return 0;
            }

            Think();
            GameEventMgr.Notify(GameNPCEvent.OnAICallback, m_body);
            return ThinkInterval;
        }
	}
}
