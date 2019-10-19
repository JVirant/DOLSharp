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
        	AggroLink = 0;
        }

        public AmteMobBrain(ABrain brain)
        {
            if (!(brain is IOldAggressiveBrain))
                return;
            var old = (IOldAggressiveBrain)brain;
            m_aggroLevel = old.AggroLevel;
            m_aggroMaxRange = old.AggroRange;
        }

        #region Bring a Friend (Link)
        /// <summary>
        /// BAF range for adds close to the pulled mob.
        /// </summary>
        public override ushort BAFCloseRange
        {
            get { return (ushort)(AggroRange / 2); }
        }

        /// <summary>
        /// BAF range for group adds in dungeons.
        /// </summary>
        public override ushort BAFReinforcementsRange
        {
            get { return m_BAFReinforcementsRange; }
            set { m_BAFReinforcementsRange = (value > 0) ? value : (ushort)0; }
        }

        /// <summary>
        /// Range for potential targets around the puller.
        /// </summary>
        public override ushort BAFTargetPlayerRange
        {
            get { return m_BAFTargetPlayerRange; }
            set { m_BAFTargetPlayerRange = (value > 0) ? value : (ushort)0; }
        }

        /// <summary>
        /// Bring friends when this living is attacked. There are 2
        /// different mechanisms for BAF:
        /// 1) Any mobs of the same faction within a certain (short) range
        ///    around the pulled mob will add on the puller, anywhere.
        /// 2) In dungeons, group size is taken into account as well, the
        ///    bigger the group, the more adds will come, even if they are
        ///    not close to the pulled mob.
        /// </summary>
        /// <param name="attackData">The data associated with the puller's attack.</param>
        protected override void BringFriends(AttackData attackData)
        {
            // Only add on players.
            var attacker = attackData.Attacker;
            if (attacker is GameNPC && ((GameNPC) attacker).Brain is IControlledBrain)
                attacker = ((IControlledBrain) ((GameNPC) attacker).Brain).GetPlayerOwner();
            if (!(attacker is GamePlayer))
                return;

            BringReinforcements(attacker as GamePlayer);
        }

        /// <summary>
        /// Get mobs to add on the puller's group, their numbers depend on the
        /// group's size.
        /// </summary>
        protected virtual void BringReinforcements(GamePlayer attacker)
        {
			int attackerCount;
			lock (attacker.Attackers)
				attackerCount = attacker.Attackers.Where(o => o is GameNPC npc ? npc.IsFriend(Body) : o.Name == Body.Name || o.GuildName == Body.GuildName).Count();
			if (AggroLink == -1)
			{
				var attackerGroup = attacker.Group;
				var numAttackers = ((attackerGroup == null) ? 1 : attackerGroup.MemberCount) + Util.Random(-3, 2).Clamp(0, 2);
				if (attackerGroup != null)
				{
					var members = attackerGroup.GetMembersInTheGroup();
					foreach (var member in members)
						if (member.ControlledBrain != null && member.ControlledBrain.Body != null)
							numAttackers += 1;
				}

				var maxAdds = Math.Max(1, (numAttackers + 1) / 2 - 1);

				var numAdds = attackerCount * 8 / 10;
				ushort range = 256;

				while (numAdds < maxAdds && range <= BAFReinforcementsRange)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(range))
					{
						if (!npc.IsFriend(Body) || !npc.IsAggressive || !npc.IsAvailable)
							continue;
						if (npc.Brain is StandardMobBrain brain)
						{
							brain.AddToAggroList(PickTarget(attacker), 1);
							if (brain is AmteMobBrain amteMobBrain)
								amteMobBrain.AttackMostWanted();
							if (++numAdds >= maxAdds)
								break;
						}
					}

					// we remove one add everytime we increase the range
					++numAdds;
					// Increase the range for finding friends to join the fight.
					range *= 2;
				}
			}
			else
			{
				var data = Body.GetNPCsInRadius(false, BAFReinforcementsRange, true, false)
					.OfType<NPCDistEntry>()
					.Where(n => n.NPC.IsAggressive && n.NPC.IsAvailable && n.NPC.IsFriend(Body))
					.OrderBy(o => o.Distance)
					.ToList();
				int need = AggroLink - attackerCount;
				for (; need > 0 && data.Count > 0; --need)
				{
					var brain = data[0].NPC.Brain as AmteMobBrain;
					if (brain != null)
					{
						brain.AddToAggroList(PickTarget(attacker), 1);
						brain.AttackMostWanted();
						data.RemoveAt(0);
					}
				}
			}
        }

        #endregion

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
				return 0;	// only attack if green+ to target

			if (Body.Faction != null && target is GamePlayer)
			{
				GamePlayer player = (GamePlayer)target;
				AggroLevel = Body.Faction.GetAggroToFaction(player);
			}
			if (AggroLevel >= 100)
				return 100;
			return AggroLevel;
		}

		public override void CheckAbilities()
		{
			////load up abilities
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
