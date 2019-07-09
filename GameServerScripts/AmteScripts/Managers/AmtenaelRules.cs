using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmteScripts.Managers;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.AI.Brain;
using DOL.GS.Keeps;

namespace DOL.GS.ServerRules
{
    [ServerRules(eGameServerType.GST_PvP)]
    public class AmtenaelRules : PvPServerRules
    {
    	public static ushort HousingRegionID = 202;

        public override string RulesDescription()
        {
            return "Règles d'Amtenaël (PvP + RvR)";
        }

        public override void OnReleased(Events.DOLEvent e, object sender, EventArgs args)
        {
            if (RvrManager.Instance.IsInRvr(sender as GameLiving))
                return;
            base.OnReleased(e, sender, args);
        }

		private bool _IsAllowedToAttack_PvpImmunity(GameLiving attacker, GamePlayer playerAttacker, GamePlayer playerDefender, bool quiet)
		{
			if (playerDefender != null)
			{
				if (playerDefender.Client.ClientState == GameClient.eClientState.WorldEnter)
				{
					if (!quiet)
						MessageToLiving(attacker, playerDefender.Name + " est en train de se connecter, vous ne pouvez pas l'attaquer pour le moment.");
					return false;
				}

				if (playerAttacker != null)
				{
					// Attacker immunity
					if (playerAttacker.IsInvulnerableToAttack)
					{
						if (quiet == false)
							MessageToLiving(attacker,
							                "You can't attack players until your PvP invulnerability timer wears off!");
						return false;
					}

					// Defender immunity
					if (playerDefender.IsInvulnerableToAttack)
					{
						if (quiet == false)
							MessageToLiving(attacker, playerDefender.Name + " is temporarily immune to PvP attacks!");
						return false;
					}
				}
			}
			return true;
		}

    	public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
        {
            if (attacker == null || defender == null)
                return false;

            //dead things can't attack
			if (!defender.IsAlive || !attacker.IsAlive)
				return false;

			if (attacker == defender)
			{
				if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas vous attaquer vous-même.");
				return false;
			}

        	// PEACE NPCs can't be attacked/attack
			if ((attacker is GameNPC && (((GameNPC)attacker).Flags & GameNPC.eFlags.PEACE) != 0) ||
				(defender is GameNPC && (((GameNPC)defender).Flags & GameNPC.eFlags.PEACE) != 0))
				return false;

            var playerAttacker = attacker as GamePlayer;
            var playerDefender = defender as GamePlayer;

            // if Pet, let's define the controller once
    		if (defender is GameNPC && (defender as GameNPC).Brain is IControlledBrain)
    			playerDefender = ((defender as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

    		if (attacker is GameNPC && (attacker as GameNPC).Brain is IControlledBrain)
    		{
    			playerAttacker = ((attacker as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
    			quiet = false;
    		}

    		if (playerDefender != null && playerDefender == playerAttacker)
			{
				if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas vous attaquer vous-même.");
				return false;
			}

			if (playerDefender != null && playerAttacker != null &&
				(attacker.CurrentRegionID == HousingRegionID || defender.CurrentRegionID == HousingRegionID))
				return false;

            if (!_IsAllowedToAttack_PvpImmunity(attacker, playerAttacker, playerDefender, quiet))
                return false;

            // Your pet can only attack stealthed players you have selected
            if (defender.IsStealthed && attacker is GameNPC)
                if (((attacker as GameNPC).Brain is IControlledBrain) &&
                    defender is GamePlayer &&
                    attacker.TargetObject != defender)
                    return false;

            //Checking for shadowed necromancer, can't be attacked.
            if (defender.ControlledBrain != null && defender.ControlledBrain.Body != null && defender.ControlledBrain.Body is NecromancerPet)
            {
                if (quiet == false) MessageToLiving(attacker, "You can't attack a shadowed necromancer!");
                return false;
            }

            // Pets
            if (attacker is GameNPC)
            {
                var controlled = ((GameNPC)attacker).Brain as IControlledBrain;
                if (controlled != null)
                {
                    attacker = controlled.GetPlayerOwner();
                    quiet = true; // silence all attacks by controlled npc
                }
            }
            if (defender is GameNPC)
            {
                var controlled = ((GameNPC)defender).Brain as IControlledBrain;
                if (controlled != null)
                    defender = controlled.GetPlayerOwner();
            }

            // RvR Rules
            if (RvrManager.Instance != null && RvrManager.Instance.IsInRvr(attacker))
                return RvrManager.Instance.IsAllowedToAttack(attacker, defender, quiet);

            // Safe area
            if (attacker is GamePlayer && defender is GamePlayer)
            {
                if (defender.CurrentAreas.Cast<AbstractArea>().Any(area => area.IsSafeArea) ||
                    attacker.CurrentAreas.Cast<AbstractArea>().Any(area => area.IsSafeArea))
                {
                    if (quiet == false)
                        MessageToLiving(attacker, "Vous ne pouvez pas attaquer quelqu'un dans une zone safe !");
                    return false;
                }
            }

            // PVP)
            if (playerAttacker != null && playerDefender != null)
            {
            	//check group
                if (playerAttacker.Group != null && playerAttacker.Group.IsInTheGroup(playerDefender))
                {
                    if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre groupe.");
                    return false;
                }

                if (playerAttacker.DuelTarget != defender)
                {
                    //check guild
                    if (playerAttacker.Guild != null && playerAttacker.Guild == playerDefender.Guild)
                    {
                        if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre guilde.");
                        return false;
                    }

                    // Player can't hit other members of the same BattleGroup
                    var mybattlegroup = (BattleGroup)playerAttacker.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                    if (mybattlegroup != null && mybattlegroup.IsInTheBattleGroup(playerDefender))
                    {
                        if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre groupe de combat.");
                        return false;
                    }
                }
            }

            //GMs can't be attacked
            if (playerDefender != null && playerDefender.Client.Account.PrivLevel > 1)
                return false;

            // allow mobs to attack mobs
			if (attacker.Realm == 0 && defender.Realm == 0)
			{
				if (attacker is GameNPC && !((GameNPC)attacker).IsConfused &&
					defender is GameNPC && !((GameNPC)defender).IsConfused)
					return !((GameNPC) attacker).IsFriend((GameNPC) defender);
				return true;
			}
    		if ((attacker.Realm != 0 || defender.Realm != 0) && playerDefender == null && playerAttacker == null)
                return true;

            //allow confused mobs to attack same realm
            if (attacker is GameNPC && (attacker as GameNPC).IsConfused && attacker.Realm == defender.Realm)
                return true;

            // "friendly" NPCs can't attack "friendly" players
            if (defender is GameNPC && defender.Realm != 0 && attacker.Realm != 0 && defender is GameKeepGuard == false && defender is GameFont == false)
            {
                if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas attaquer un PNJ amical.");
                return false;
            }
            // "friendly" NPCs can't be attacked by "friendly" players
            if (attacker is GameNPC && attacker.Realm != 0 && defender.Realm != 0 && attacker is GameKeepGuard == false)
                return false;

            return true;
        }

        public override bool IsSameRealm(GameLiving source, GameLiving target, bool quiet)
        {
            if (source == null || target == null)
                return false;
            if (target is GameNPC)
                if ((((GameNPC)target).Flags & GameNPC.eFlags.PEACE) != 0)
                    return true;

            if (source is GameNPC)
                if ((((GameNPC)source).Flags & GameNPC.eFlags.PEACE) != 0)
                    return true;
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            return base.IsSameRealm(source, target, quiet);
        }

		public override bool CheckAbilityToUseItem(GameLiving living, ItemTemplate item)
		{
			if (living == null || item == null)
				return false;

			GamePlayer player = living as GamePlayer;

			// GMs can equip everything
			if (player != null && player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
				return true;

			// allow usage of all house items
			if ((item.Object_Type == 0 || item.Object_Type >= (int)eObjectType._FirstHouse) && item.Object_Type <= (int)eObjectType._LastHouse)
				return true;

			// on some servers we may wish for dropped items to be used by all realms regardless of what is set in the db
			if (!Properties.ALLOW_CROSS_REALM_ITEMS && item.Realm != 0 && item.Realm != (int)living.Realm)
				return false;

			// classes restriction. 0 means every class
			if (player != null && !Util.IsEmpty(item.AllowedClasses, true) && !item.AllowedClasses.SplitCSV(true).Contains(player.CharacterClass.ID.ToString()))
				return false;

			//armor
			if (item.Object_Type >= (int)eObjectType._FirstArmor && item.Object_Type <= (int)eObjectType._LastArmor)
			{
				int armorAbility = -1;
				switch ((eRealm)item.Realm)
				{
					case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
					case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
					case eRealm.Midgard: armorAbility = living.GetAbilityLevel(Abilities.MidArmor); break;
					default: // use old system
						armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.AlbArmor));
						armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.HibArmor));
						armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.MidArmor));
						break;
				}
				switch ((eObjectType)item.Object_Type)
				{
					case eObjectType.GenericArmor: return armorAbility >= ArmorLevel.GenericArmor;
					case eObjectType.Cloth: return armorAbility >= ArmorLevel.Cloth;
					case eObjectType.Leather: return armorAbility >= ArmorLevel.Leather;
					case eObjectType.Reinforced:
					case eObjectType.Studded: return armorAbility >= ArmorLevel.Studded;
					case eObjectType.Scale:
					case eObjectType.Chain: return armorAbility >= ArmorLevel.Chain;
					case eObjectType.Plate: return armorAbility >= ArmorLevel.Plate;
					default: return false;
				}
			}

			// non-armors
			string abilityCheck = null;
			string[] otherCheck = new string[0];

			//http://dol.kitchenhost.de/files/dol/Info/itemtable.txt
			switch ((eObjectType)item.Object_Type)
			{
				case eObjectType.GenericItem: return true;
				case eObjectType.GenericArmor: return true;
				case eObjectType.GenericWeapon: return true;
				case eObjectType.Staff: abilityCheck = Abilities.Weapon_Staves; break;
				case eObjectType.Fired: abilityCheck = Abilities.Weapon_Shortbows; break;
				case eObjectType.FistWraps: abilityCheck = Abilities.Weapon_FistWraps; break;
				case eObjectType.MaulerStaff: abilityCheck = Abilities.Weapon_MaulerStaff; break;

				//alb
				case eObjectType.CrushingWeapon: abilityCheck = Abilities.Weapon_Crushing; break;
				case eObjectType.SlashingWeapon: abilityCheck = Abilities.Weapon_Slashing; break;
				case eObjectType.ThrustWeapon: abilityCheck = Abilities.Weapon_Thrusting; break;
				case eObjectType.TwoHandedWeapon: abilityCheck = Abilities.Weapon_TwoHanded; break;
				case eObjectType.PolearmWeapon: abilityCheck = Abilities.Weapon_Polearms; break;
				case eObjectType.Longbow:
					otherCheck = new[] { Abilities.Weapon_Longbows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Crossbow: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Flexible: abilityCheck = Abilities.Weapon_Flexible; break;
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;

				//mid
				case eObjectType.Sword: abilityCheck = Abilities.Weapon_Swords; break;
				case eObjectType.Hammer: abilityCheck = Abilities.Weapon_Hammers; break;
				case eObjectType.LeftAxe:
				case eObjectType.Axe: abilityCheck = Abilities.Weapon_Axes; break;
				case eObjectType.Spear: abilityCheck = Abilities.Weapon_Spears; break;
				case eObjectType.CompositeBow:
					otherCheck = new[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Thrown: abilityCheck = Abilities.Weapon_Thrown; break;
				case eObjectType.HandToHand: abilityCheck = Abilities.Weapon_HandToHand; break;

				//hib
				case eObjectType.RecurvedBow:
					otherCheck = new[] { Abilities.Weapon_RecurvedBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Blades: abilityCheck = Abilities.Weapon_Blades; break;
				case eObjectType.Blunt: abilityCheck = Abilities.Weapon_Blunt; break;
				case eObjectType.Piercing: abilityCheck = Abilities.Weapon_Piercing; break;
				case eObjectType.LargeWeapons: abilityCheck = Abilities.Weapon_LargeWeapons; break;
				case eObjectType.CelticSpear: abilityCheck = Abilities.Weapon_CelticSpear; break;
				case eObjectType.Scythe: abilityCheck = Abilities.Weapon_Scythe; break;

				//misc
				case eObjectType.Magical: return true;
				case eObjectType.Shield: return living.GetAbilityLevel(Abilities.Shield) >= item.Type_Damage;
				case eObjectType.Bolt: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Arrow: otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Longbows, Abilities.Weapon_RecurvedBows, Abilities.Weapon_Shortbows }; break;
				case eObjectType.Poison: return living.GetModifiedSpecLevel(Specs.Envenom) > 0;
				case eObjectType.Instrument: return living.HasAbility(Abilities.Weapon_Instruments);
				//TODO: different shield sizes
			}

			if (abilityCheck != null && living.HasAbility(abilityCheck))
				return true;

			foreach (string str in otherCheck)
				if (living.HasAbility(str))
					return true;

			return false;
		}

        public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet)
        {
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            return true;
        }

        public override bool IsAllowedToJoinGuild(GamePlayer source, Guild guild)
        {
            if (RvrManager.Instance.IsInRvr(source))
                return source.Realm == guild.Realm;
            return true;
        }

        public override bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet)
        {
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            return true;
        }

        public override bool IsAllowedToUnderstand(GameLiving source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            return true;
        }

        public override string ReasonForDisallowMounting(GamePlayer player)
        {
            return RvrManager.Instance.IsInRvr(player) ? "Vous ne pouvez pas appeler votre monture ici !" : base.ReasonForDisallowMounting(player);
        }

		private class GainerList
		{
			private float _totalDmg;
			private readonly GameObject _killed;
			private readonly Dictionary<GamePlayer, float> _playerXdmg = new Dictionary<GamePlayer, float>();
			private readonly Dictionary<Group, float> _groupXdmg = new Dictionary<Group, float>();

			public GainerList(GameObject killed)
			{
				_killed = killed;
			}

			public GamePlayer AddObject(GameObject obj, float dmg)
			{
				_totalDmg += dmg;
				if (obj.ObjectState != GameObject.eObjectState.Active ||
					!_killed.IsWithinRadius(obj, WorldMgr.MAX_EXPFORKILL_DISTANCE))
					return null;
				if (!(obj is NecromancerPet))
					dmg *= 0.75f; // penality
				if (obj is GameNPC && ((GameNPC) obj).Brain is IControlledBrain)
					obj = ((IControlledBrain) ((GameNPC) obj).Brain).GetPlayerOwner();
				if (!(obj is GamePlayer))
					return null;

				var plr = (GamePlayer) obj;
				if (plr.Group != null)
				{
					var grp = plr.Group;
					if (_groupXdmg.ContainsKey(grp))
						_groupXdmg[grp] += dmg;
					else
						_groupXdmg.Add(grp, dmg);
				}
				else
				{
					if (_playerXdmg.ContainsKey(plr))
						_playerXdmg[plr] += dmg;
					else
						_playerXdmg.Add(plr, dmg);
				}
				return plr;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="playerAction">action(player, ratio, groupCount), ratio is Damages of player or his group divided by TotalDamages</param>
			public void Foreach(Action<GamePlayer, float, int> playerAction)
			{
				_playerXdmg.ForEach(kvp => playerAction(kvp.Key, kvp.Value/_totalDmg, 1));
				_groupXdmg.ForEach(
					kvp =>
					{
						var players = kvp.Key.GetPlayersInTheGroup().Where(
							p => _killed.IsWithinRadius(p, WorldMgr.MAX_EXPFORKILL_DISTANCE));
						var cnt = players.Count();
						players.ForEach(p => playerAction(p, kvp.Value/_totalDmg, cnt));
					});
			}
		}

		public override void OnNPCKilled(GameNPC killedNPC, GameObject killer)
		{
			var gainers = new GainerList(killedNPC);
			GameLiving highestPlayer = null;

			lock (killedNPC.XPGainers.SyncRoot)
			{
				#region Worth no experience

				//"This monster has been charmed recently and is worth no experience."
				string message = "You gain no experience from this kill!";
				if (killedNPC.CurrentRegion.Time - GameNPC.CHARMED_NOEXP_TIMEOUT <
				    killedNPC.TempProperties.getProperty<long>(GameNPC.CHARMED_TICK_PROP))
				{
					message = "This monster has been charmed recently and is worth no experience.";
				}

				if (!killedNPC.IsWorthReward)
				{
					foreach (DictionaryEntry de in killedNPC.XPGainers)
					{
						GamePlayer player = de.Key as GamePlayer;
						if (player != null)
							player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					return;
				}

				#endregion

				#region Group/Total Damage
				//Collect the total damage
				foreach (DictionaryEntry de in killedNPC.XPGainers)
				{
					var player = gainers.AddObject(de.Key as GameObject, (float) de.Value);

					//Check stipulations (this will ignore all pet damage)
					if (player == null)
						continue;

					// tolakram: only prepare for xp challenge code if player is in a group
					if (highestPlayer == null)
						highestPlayer = player;
					else
					{
						if (player.Group != null)
							player = player.Group.GetPlayersInTheGroup().OrderByDescending(p => p.Level).First();
						if (player.Level > highestPlayer.Level)
							highestPlayer = player;
					}
				}

				#endregion
			}

			long npcExpValue = killedNPC.ExperienceValue;
			int npcRPValue = killedNPC.RealmPointsValue;
			int npcBPValue = killedNPC.BountyPointsValue;
			double npcExceedXPCapAmount = killedNPC.ExceedXPCapAmount;

			//Need to do this before hand so we only do it once - just in case if the player levels!
			double highestConValue = 0;
			if (highestPlayer != null)
				highestConValue = highestPlayer.GetConLevel(killedNPC);

			#region Realm Points

			gainers.Foreach(
				(player, damagePercent, groupCount) =>
				{
					// realm points
					int rpCap = player.RealmPointsValue*2;
					int realmPoints;

					// Keep and Tower captures reward full RP and BP value to each player
					if (killedNPC is GuardLord)
					{
						realmPoints = npcRPValue;
					}
					else
					{
						realmPoints = (int) (npcRPValue*damagePercent);
						//rp bonuses from RR and Group
						//100% if full group,scales down according to player count in group and their range to target
						if (groupCount > 1)
							realmPoints = realmPoints*(1000 + groupCount*125)/1000;
					}

					if (realmPoints > rpCap)
						realmPoints = rpCap;

					if (realmPoints > 0)
						player.GainRealmPoints(realmPoints);
				});

			#endregion

			#region Bounty Points

			gainers.Foreach(
				(player, damagePercent, groupCount) =>
				{
					int bpCap = player.BountyPointsValue*2;
					// Keep and Tower captures reward full RP and BP value to each player
					var bountyPoints = killedNPC is GuardLord ? npcBPValue : (int) (npcBPValue*damagePercent);

					if (bountyPoints > bpCap)
						bountyPoints = bpCap;
					if (bountyPoints > 0)
						player.GainBountyPoints(bountyPoints);
				});

			#endregion

			#region Experience

			gainers.Foreach(
				(player, damagePercent, groupCount) =>
				{
					// experience points
					long groupExp = 0;
					long xpReward = (long) (npcExpValue*damagePercent);

					/* 
					 * Experience clamps have been raised from 1.1x a same level kill to 1.25x a same level kill.
					 * This change has two effects: it will allow lower level players in a group to gain more experience faster (15% faster),
					 * and it will also let higher level players (the 35-50s who tend to hit this clamp more often) to gain experience faster.
					 */
					long expCap = GetExperienceForLiving(player.Level)*Properties.XP_CAP_PERCENT/100;

					// Optional group cap can be set different from standard player cap
					if (groupCount > 1)
						expCap += expCap * groupCount * 250 / 1000;

					#region Challenge Code

					//let's check the con, for example if a level 50 kills a green, we want our level 1 to get green xp too
					/*
					 * http://www.camelotherald.com/more/110.shtml
					 * All group experience is divided evenly amongst group members, if they are in the same level range. What's a level range? One color range.
					 * If everyone in the group cons yellow to each other (or high blue, or low orange), experience will be shared out exactly evenly, with no leftover points.
					 * How can you determine a color range? Simple - Level divided by ten plus one. So, to a level 40 player (40/10 + 1), 36-40 is yellow, 31-35 is blue,
					 * 26-30 is green, and 25-less is gray. But for everyone in the group to get the maximum amount of experience possible, the encounter must be a challenge to
					 * the group. If the group has two people, the monster must at least be (con) yellow to the highest level member. If the group has four people, the monster
					 * must at least be orange. If the group has eight, the monster must at least be red.
					 *
					 * If "challenge code" has been activated, then the experience is divided roughly like so in a group of two (adjust the colors up if the group is bigger): If
					 * the monster was blue to the highest level player, each lower level group member will ROUGHLY receive experience as if they soloed a blue monster.
					 * Ditto for green. As everyone knows, a monster that cons gray to the highest level player will result in no exp for anyone. If the monster was high blue,
					 * challenge code may not kick in. It could also kick in if the monster is low yellow to the high level player, depending on the group strength of the pair.
					 */
					//xp challenge
					if (highestPlayer != null && highestConValue < 0)
					{
						//challenge success, the xp needs to be reduced to the proper con
						expCap = GetExperienceForLiving(GameObject.GetLevelFromCon(player.Level, highestConValue));
					}

					#endregion

					expCap = (long) (expCap*npcExceedXPCapAmount);

					if (xpReward > expCap)
						xpReward = expCap;

					#region Camp Bonus

					// camp bonus
					double fullCampBonus = Properties.MAX_CAMP_BONUS;
					const double fullCampBonusTicks = 600000; //1 hour (in ms) = full 100%
					long livingLifeSpan = killedNPC.CurrentRegion.Time - killedNPC.SpawnTick;
					double campBonusPerc = fullCampBonus*(livingLifeSpan/fullCampBonusTicks);
					//1.49 http://news-daoc.goa.com/view_patchnote_archive.php?id_article=2478
					//"Camp bonuses" have been substantially upped in dungeons. Now camp bonuses in dungeons are, on average, 20% higher than outside camp bonuses.
					if (killer.CurrentZone.IsDungeon)
						campBonusPerc += 0.20;

					if (campBonusPerc < 0.01)
						campBonusPerc = 0;
					else if (campBonusPerc > fullCampBonus)
						campBonusPerc = fullCampBonus;

					long campBonus = (long) (xpReward*campBonusPerc);

					#endregion

					if (xpReward <= 0)
						return;

					if (groupCount > 1)
						groupExp = xpReward*groupCount*625/10000;

					// tolakram - remove this for now.  Correct calculation should be reduced XP based on damage pet did, not a flat reduction
					//if (player.ControlledNpc != null)
					//    xpReward = (long)(xpReward * 0.75);

					//Ok we've calculated all the base experience.  Now let's add them all together.
					xpReward += campBonus + groupExp;

					if (!player.IsAlive) //Dead living gets 25% exp only
						xpReward = xpReward*25/100;

					//XP Rate is handled in GainExperience
					player.GainExperience(GameLiving.eXPSource.NPC, xpReward, campBonus, groupExp, 0, true, true, true);
				});

			#endregion
		}
    }
}
