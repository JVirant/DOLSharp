using System;
using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
	public class SimpleGvGGuard : AmteMob
	{
		public SimpleGvGGuard()
		{
			var brain = new SimpleGvGGuardBrain();
			brain.AggroLink = 3;
			SetOwnBrain(brain);
		}

	}
}

namespace DOL.AI.Brain
{
	public class SimpleGvGGuardBrain : AmteMobBrain
	{
		public override int AggroLevel
		{
			get { return 100; }
			set { }
		}

		protected override void CheckPlayerAggro()
		{
			if (Body.AttackState)
				return;
			foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
					continue;

				int aggro = CalculateAggroLevelToTarget(pl);
				if (aggro <= 0)
					continue;
				AddToAggroList(pl, aggro);
				if (pl.Level > Body.Level - 20 || (pl.Group != null && pl.Group.MemberCount > 2))
					BringReinforcements(pl);
			}
		}

		protected override void CheckNPCAggro()
		{
			if (Body.AttackState)
				return;
			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, Body.CurrentRegion.IsDungeon ? false : true))
			{
				if (npc.Realm != 0 || (npc.Flags & GameNPC.eFlags.PEACE) != 0 ||
					!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active ||
					npc is GameTaxi ||
					m_aggroTable.ContainsKey(npc) ||
					!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
					continue;

				int aggro = CalculateAggroLevelToTarget(npc);
				if (aggro <= 0)
					continue;
				AddToAggroList(npc, aggro);
				if (npc.Level > Body.Level)
					BringReinforcements(npc);
			}
		}

		private void BringReinforcements(GameNPC target)
		{
			int count = (int)Math.Log(target.Level - Body.Level, 2) + 1;
			foreach (GameNPC npc in Body.GetNPCsInRadius(WorldMgr.YELL_DISTANCE))
			{
				if (count <= 0)
					return;
				if (npc.Brain is SimpleGvGGuardBrain == false)
					continue;
				var brain = npc.Brain as SimpleGvGGuardBrain;
				brain.AddToAggroList(target, 1);
				brain.AttackMostWanted();
			}
		}

		public override int CalculateAggroLevelToTarget(GameLiving target)
		{
			if (target is AmtePlayer)
				return target.GuildName == Body.GuildName ? 0 : 100;
			if (target.Realm == 0)
				return 0;
			return base.CalculateAggroLevelToTarget(target);
		}
	}
}
