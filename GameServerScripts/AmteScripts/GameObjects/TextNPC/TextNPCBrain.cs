using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
	public class TextNPCBrain : AmteMobBrain
	{
		public override int ThinkInterval
		{
			get { return 1000; }
		}

		public override void Think()
		{
			base.Think();
			if(Body is ITextNPC && !Body.InCombat)
				((ITextNPC)Body).SayRandomPhrase();
		}
	}
}
