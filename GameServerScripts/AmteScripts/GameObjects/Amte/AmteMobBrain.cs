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
			if (AggroLink == -1)
			{
				var attackerGroup = attacker.Group;
				var numAttackers = (attackerGroup == null) ? 1 : attackerGroup.MemberCount;
				var maxAdds = Math.Max(1, (numAttackers + 1) / 2 - 1);

				var numAdds = 0;
				ushort range = 256;

				while (numAdds < maxAdds && range <= BAFReinforcementsRange)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(range))
					{
						if (!npc.IsFriend(Body) || !npc.IsAggressive || !npc.IsAvailable)
							continue;

						var brain = npc.Brain as AmteMobBrain;
						if (brain != null)
						{
							brain.AddToAggroList(PickTarget(attacker), 1);
							brain.AttackMostWanted();
							if (++numAdds >= maxAdds)
								break;
						}
					}

					// Increase the range for finding friends to join the fight.
					range *= 2;
				}
			}
			else
			{
				int need;
				lock (attacker.Attackers)
					need = AggroLink - attacker.Attackers.Where(o => o.Name == Body.Name || o.GuildName == Body.GuildName).Count();
				var data = Body.GetNPCsInRadius(false, BAFReinforcementsRange, true, false)
					.OfType<NPCDistEntry>()
					.Where(n => n.NPC.IsAggressive && n.NPC.IsAvailable && n.NPC.IsFriend(Body))
					.OrderBy(o => o.Distance)
					.ToList();
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

		#region Defensive Spells
		/// <summary>
		/// Checks defensive spells.  Handles buffs, heals, etc.
		/// </summary>
		protected override bool CheckDefensiveSpells(Spell spell)
		{
			if (spell == null) return false;
			if (Body.GetSkillDisabledDuration(spell) > 0) return false;
			GameObject lastTarget = Body.TargetObject;
			Body.TargetObject = null;
			switch (spell.SpellType)
			{
				#region Buffs
				case "StrengthConstitutionBuff":
				case "DexterityQuicknessBuff":
				case "StrengthBuff":
				case "DexterityBuff":
				case "ConstitutionBuff":
				case "ArmorFactorBuff":
				case "ArmorAbsorptionBuff":
				case "CombatSpeedBuff":
				case "MeleeDamageBuff":
				case "AcuityBuff":
				case "HealthRegenBuff":
				case "DamageAdd":
				case "DamageShield":
				case "BodyResistBuff":
				case "ColdResistBuff":
				case "EnergyResistBuff":
				case "HeatResistBuff":
				case "MatterResistBuff":
				case "SpiritResistBuff":
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff":
				case "CrushSlashThrustBuff":
				case "AllMagicResistsBuff":
				case "AllMeleeResistsBuff":
				case "AllResistsBuff":
				case "OffensiveProc":
				case "DefensiveProc":
				case "Bladeturn":
				case "ToHitBuff":
					{
						// Buff self, if not in melee, but not each and every mob
						// at the same time, because it looks silly.
						if (!LivingHasEffect(Body, spell) && !Body.AttackState && Util.Chance(80))
						{
							Body.TargetObject = Body;
							break;
						}
						if (!Body.InCombat && spell.Target == "realm")
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
								if (Body.IsFriend(npc) && !LivingHasEffect(npc, spell))
								{
									Body.TargetObject = npc;
									break;
								}
						}
						break;
					}
				#endregion Buffs

				#region Disease Cure/Poison Cure/Summon
				case "CureDisease":
					if (Body.IsDiseased)
					{
						Body.TargetObject = Body;
						break;
					}
					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && npc.IsDiseased && Util.Chance(60))
							{
								Body.TargetObject = npc;
								break;
							}
					}
					break;
				case "CurePoison":
					if (LivingIsPoisoned(Body))
					{
						Body.TargetObject = Body;
						break;
					}
					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && LivingIsPoisoned(npc) && Util.Chance(60))
							{
								Body.TargetObject = npc;
								break;
							}
					}
					break;
				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonTheurgistPet":
					break;
				case "Summon":
				case "SummonCommander":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonMastery":
				case "SummonMercenary":
				case "SummonMonster":
				case "SummonNoveltyPet":
				case "SummonSalamander":
				case "SummonSiegeWeapon":
				case "SummonSimulacrum":
				case "SummonSpiritFighter":
				case "SummonTitan":
				case "SummonUnderhill":
				case "SummonWarcrystal":
				case "SummonWood":
					//Body.TargetObject = Body;
					break;
				case "SummonMinion":
					//If the list is null, lets make sure it gets initialized!
					if (Body.ControlledNpcList == null)
						Body.InitControlledBrainArray(2);
					else
					{
						//Let's check to see if the list is full - if it is, we can't cast another minion.
						//If it isn't, let them cast.
						IControlledBrain[] icb = Body.ControlledNpcList;
						int numberofpets = icb.Count(t => t != null);
						if (numberofpets >= icb.Length)
							break;
					}
					Body.TargetObject = Body;
					break;
				#endregion

				#region Heals
				case "Heal":
					// Chance to heal self when dropping below 30%, do NOT spam it.

					if (Body.HealthPercent < 70 && Util.Chance(80))
					{
						Body.TargetObject = Body;
						break;
					}

					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && npc.HealthPercent < 70)
							{
								Body.TargetObject = npc;
								break;
							}
					}

					break;
				#endregion
			}

			if (Body.TargetObject != null)
			{
				if (Body.IsMoving && spell.CastTime > 0)
					Body.StopFollowing();

				if (Body.TargetObject != Body && spell.CastTime > 0)
					Body.TurnTo(Body.TargetObject);

				Body.CastSpell(spell, m_mobSpellLine);

				Body.TargetObject = lastTarget;
				return true;
			}

			Body.TargetObject = lastTarget;

			return false;
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
