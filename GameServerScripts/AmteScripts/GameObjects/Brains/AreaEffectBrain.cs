using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
	public class AreaEffectBrain : APlayerVicinityBrain
	{
		public override int ThinkInterval
		{
			get { return 1000; }
		}

		public override void Think()
		{
			if(Body is AreaEffect)
				((AreaEffect)Body).ApplyEffect();
		}
	}
}
