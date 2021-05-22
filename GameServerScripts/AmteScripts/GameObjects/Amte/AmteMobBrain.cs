using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.RealmAbilities;

namespace DOL.AI.Brain
{
    public class AmteMobBrain : StandardMobBrain
    {
    	public int AggroLink { get; set; }

        public AmteMobBrain()
        {
        	AggroLink = -1;
        }

        public AmteMobBrain(ABrain brain)
        {
            if (!(brain is IOldAggressiveBrain))
                return;
            var old = (IOldAggressiveBrain)brain;
            m_aggroLevel = old.AggroLevel;
            m_aggroMaxRange = old.AggroRange;
        }

		#region RandomWalk
		public override IPoint3D CalcRandomWalkTarget()
        {
            var roamingRadius = Body.RoamingRange > 0 ? Util.Random(0, Body.RoamingRange) : (Body.CurrentRegion.IsDungeon ? 100 : 500);
            var angle = Util.Random(0, 360) / (2 * Math.PI);
            var targetX = Body.SpawnPoint.X + Math.Cos(angle) * roamingRadius;
            var targetY = Body.SpawnPoint.Y + Math.Sin(angle) * roamingRadius;
            return new Point3D((int)targetX, (int)targetY, Body.SpawnPoint.Z);
        }
		#endregion

		public override int CalculateAggroLevelToTarget(GameLiving target)
		{
			if (GameServer.ServerRules.IsSameRealm(Body, target, true))
				return 0;

			// related to the pet owner if applicable
			if (target is GamePet)
			{
				GamePlayer thisLiving = ((IControlledBrain)((GamePet)target).Brain).GetPlayerOwner();
				if (thisLiving != null && thisLiving.IsObjectGreyCon(Body))
					return 0;
			}

			if (target.IsObjectGreyCon(Body))
				return 0;   // only attack if green+ to target

			int aggro = AggroLevel;
			if (target is GamePlayer player)
			{
				if (Body.Faction != null)
					aggro = Body.Faction.GetAggroToFaction(player);
				if (aggro > 1 && player.Client.IsDoubleAccount)
					aggro += 20;
			}
			if (aggro >= 100)
				return 100;
			return aggro;
		}

		public override void CheckAbilities()
		{
			/// load up abilities
			if (Body.Abilities != null && Body.Abilities.Count > 0)
			{
				foreach (Ability ab in Body.Abilities.Values)
				{
					switch (ab.KeyName)
					{
						case Abilities.ChargeAbility:
							{
								if (Body.TargetObject is GameLiving
									&& !Body.IsWithinRadius(Body.TargetObject, 500)
									&& GameServer.ServerRules.IsAllowedToAttack(Body, Body.TargetObject as GameLiving, true))
								{
									ChargeAbility charge = Body.GetAbility<ChargeAbility>();
									if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
									{
										charge.Execute(Body);
									}
								}
								break;
							}
					}
				}
			}
		}

		protected override void AttackMostWanted()
		{
			base.AttackMostWanted();
			if (!Body.IsCasting)
				CheckAbilities();
		}
	}
}
