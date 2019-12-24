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
using DOL.GS.Scripts;
using log4net;
using System.Reflection;

namespace DOL.GS.ServerRules
{
	[ServerRules(eGameServerType.GST_PvP)]
	public class AmtenaelRules : PvPServerRules
	{
		public static ushort HousingRegionID = 202;
		public static ushort[] UnsafeRegions = new ushort[] { 181 };

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

				if (playerAttacker != null && !PvpManager.Instance.IsIn(playerAttacker) && !UnsafeRegions.Contains(playerAttacker.CurrentRegionID))
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

			//GMs can't be attacked
			if (playerDefender != null && playerDefender.Client.Account.PrivLevel > 1)
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
					attacker = controlled.GetLivingOwner() ?? attacker;
					quiet = true; // silence all attacks by controlled npc
				}
			}
			if (defender is GameNPC)
			{
				var controlled = ((GameNPC)defender).Brain as IControlledBrain;
				if (controlled != null)
					defender = controlled.GetLivingOwner() ?? defender;
			}

			if (playerAttacker != null && JailMgr.IsPrisoner(playerAttacker))
			{
				if (quiet == false)
					MessageToLiving(attacker, "Vous ne pouvez pas attaquer lorsque vous êtes en prison.");
				return false;
			}
			if (playerDefender != null && JailMgr.IsPrisoner(playerDefender))
			{
				if (quiet == false)
					MessageToLiving(attacker, "Vous ne pouvez pas attaquer un prisonnier.");
				return false;
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

			// Simple GvG Guards
			if (defender is SimpleGvGGuard && (defender.GuildName == attacker.GuildName || (playerAttacker != null && playerAttacker.GuildName == defender.GuildName)))
				return false;
			if (attacker is SimpleGvGGuard && (defender.GuildName == attacker.GuildName || (playerDefender != null && playerDefender.GuildName == attacker.GuildName)))
				return false;

			// allow mobs to attack mobs
			if (attacker.Realm == 0 && defender.Realm == 0)
			{
				if (attacker is GameNPC && !((GameNPC)attacker).IsConfused &&
					defender is GameNPC && !((GameNPC)defender).IsConfused)
					return !((GameNPC)attacker).IsFriend((GameNPC)defender);
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

			if (source.Attackers.Contains(target))
				return false;

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

		public override bool IsAllowedToCastSpell(GameLiving caster, GameLiving target, Spell spell, SpellLine spellLine)
		{
			if ((caster is GamePlayer plc && JailMgr.IsPrisoner(plc)) || (target is GamePlayer plt && JailMgr.IsPrisoner(plt)))
				return false;
			return base.IsAllowedToCastSpell(caster, target, spell, spellLine);
		}

		public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet)
		{
			if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
				return source.Realm == target.Realm;
			if (JailMgr.IsPrisoner(source) || JailMgr.IsPrisoner(target))
				return false;
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
			if ((source is GamePlayer pls && JailMgr.IsPrisoner(pls)) || (target is GamePlayer plt && JailMgr.IsPrisoner(plt)))
				return false;
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

		public override string GetPlayerName(GamePlayer source, GamePlayer target)
		{
			if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
				return source.Client.RaceToTranslatedName(target.Race, target.Gender == eGender.Male ? 1 : 2);
			return base.GetPlayerName(source, target);
		}
		public override string GetPlayerLastName(GamePlayer source, GamePlayer target)
		{
			if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
				return "";
			return base.GetPlayerLastName(source, target);
		}
		public override string GetPlayerPrefixName(GamePlayer source, GamePlayer target)
		{
			if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
				return "";
			return base.GetPlayerPrefixName(source, target);
		}
		public override string GetPlayerTitle(GamePlayer source, GamePlayer target)
		{
			if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
				return "";
			return base.GetPlayerTitle(source, target);
		}
		public override byte GetColorHandling(GameClient client)
		{
			if (client.Player != null && RvrManager.Instance.IsInRvr(client.Player))
				return 0;
			return base.GetColorHandling(client);
		}

		public virtual void OnPlayerKilled(GamePlayer killedPlayer, GameObject killer)
		{
			if (Properties.ENABLE_WARMAPMGR && killer is GamePlayer && killer.CurrentRegion.ID == 163)
				WarMapMgr.AddFight((byte)killer.CurrentZone.ID, killer.X, killer.Y, (byte)killer.Realm, (byte)killedPlayer.Realm);

			killedPlayer.LastDeathRealmPoints = 0;
			// "player has been killed recently"
			long noExpSeconds = ServerProperties.Properties.RP_WORTH_SECONDS;
			if (killedPlayer.DeathTime + noExpSeconds > killedPlayer.PlayedTime)
			{
				lock (killedPlayer.XPGainers)
				{
					foreach (var de in killedPlayer.XPGainers)
					{
						if (de.Key is GamePlayer)
						{
							((GamePlayer)de.Key).Out.SendMessage(killedPlayer.Name + " has been killed recently and is worth no realm points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							((GamePlayer)de.Key).Out.SendMessage(killedPlayer.Name + " has been killed recently and is worth no experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
					}
				}
				return;
			}

			lock (killedPlayer.XPGainers)
			{
				bool dealNoXP = false;
				var totalDamage = 0.0;
				//Collect the total damage
				foreach (var de in killedPlayer.XPGainers)
				{
					GameObject obj = (GameObject)de.Key;
					if (obj is GamePlayer)
					{
						//If a gameplayer with privlevel > 1 attacked the
						//mob, then the players won't gain xp ...
						if (((GamePlayer)obj).Client.Account.PrivLevel > 1)
						{
							dealNoXP = true;
							break;
						}
					}
					totalDamage += de.Value;
				}

				if (dealNoXP)
				{
					foreach (var de in killedPlayer.XPGainers)
					{
						GamePlayer player = de.Key as GamePlayer;
						if (player != null)
							player.Out.SendMessage("You gain no experience from this kill!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					return;
				}


				long playerExpValue = killedPlayer.ExperienceValue;
				playerExpValue = (long)(playerExpValue * Properties.XP_RATE);
				int playerRPValue = killedPlayer.RealmPointsValue;
				int playerBPValue = 0;

				bool BG = false;
				if (!Properties.ALLOW_BPS_IN_BGS)
				{
					foreach (AbstractGameKeep keep in GameServer.KeepManager.GetKeepsOfRegion(killedPlayer.CurrentRegionID))
					{
						if (keep.DBKeep.BaseLevel < 50)
						{
							BG = true;
							break;
						}
					}
				}
				if (!BG)
					playerBPValue = killedPlayer.BountyPointsValue;
				long playerMoneyValue = killedPlayer.MoneyValue;

				List<KeyValuePair<GamePlayer, int>> playerKillers = new List<KeyValuePair<GamePlayer, int>>();

				//Now deal the XP and RPs to all livings
				foreach (var de in killedPlayer.XPGainers)
				{
					GameLiving living = de.Key as GameLiving;
					GamePlayer expGainPlayer = living as GamePlayer;
					if (living == null) continue;
					if (living.ObjectState != GameObject.eObjectState.Active) continue;
					/*
					 * http://www.camelotherald.com/more/2289.shtml
					 * Dead players will now continue to retain and receive their realm point credit
					 * on targets until they release. This will work for solo players as well as
					 * grouped players in terms of continuing to contribute their share to the kill
					 * if a target is being attacked by another non grouped player as well.
					 */
					//if (!living.Alive) continue;
					if (!living.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;


					double damagePercent = de.Value / totalDamage;
					if (!living.IsAlive)//Dead living gets 25% exp only
						damagePercent *= 0.25;

					// realm points
					int rpCap = living.RealmPointsValue * 2;
					int realmPoints = (int)(playerRPValue * damagePercent);
					//rp bonuses from RR and Group
					//20% if R1L0 char kills RR10,if RR10 char kills R1L0 he will get -20% bonus
					//100% if full group,scales down according to player count in group and their range to target
					if (living is GamePlayer killerPlayer)
					{
						//only gain rps in a battleground if you are under the cap
						Battleground bg = GameServer.KeepManager.GetBattleground(killerPlayer.CurrentRegionID);
						if (bg == null || (killerPlayer.RealmLevel < bg.MaxRealmLevel))
						{
							realmPoints = (int)(realmPoints * (1.0 + 2.0 * (killedPlayer.RealmLevel - killerPlayer.RealmLevel) / 900.0));
							if (killerPlayer.Group != null && killerPlayer.Group.MemberCount > 1)
							{
								lock (killerPlayer.Group)
								{
									int count = 0;
									foreach (GamePlayer player in killerPlayer.Group.GetPlayersInTheGroup())
									{
										if (!player.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;
										count++;
									}
									realmPoints = (int)(realmPoints * (1.0 + count * 0.125));
								}
							}
						}

						if (RvrManager.Instance.IsInRvr(killerPlayer))
						{
							var bonus = 0.1;
							var lords = RvrManager.Instance.Lords;
							foreach (var lord in lords)
								if (killerPlayer.GetDistance(lord) < 4000)
									bonus += 0.5;
							if (lords.Any(l => l.GuildName == killerPlayer.GuildName))
								bonus += 0.5;
							realmPoints += (int)(realmPoints * bonus);
							rpCap += (int)(rpCap * bonus);
						}

						if (realmPoints > rpCap)
							realmPoints = rpCap;
						if (realmPoints > 0)
						{
							if (living is GamePlayer)
							{
								killedPlayer.LastDeathRealmPoints += realmPoints;
								playerKillers.Add(new KeyValuePair<GamePlayer, int>(living as GamePlayer, realmPoints));
							}

							living.GainRealmPoints(realmPoints);
						}
					}

					// bounty points
					int bpCap = living.BountyPointsValue * 2;
					int bountyPoints = (int)(playerBPValue * damagePercent);
					if (bountyPoints > bpCap)
						bountyPoints = bpCap;

					//FIXME: [WARN] this is guessed, i do not believe this is the right way, we will most likely need special messages to be sent
					//apply the keep bonus for bounty points
					if (killer != null)
					{
						if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_5, (eRealm)killer.Realm))
							bountyPoints += (bountyPoints / 100) * 5;
						else if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_3, (eRealm)killer.Realm))
							bountyPoints += (bountyPoints / 100) * 3;
					}

					if (bountyPoints > 0)
					{
						living.GainBountyPoints(bountyPoints);
					}

					// experience
					// TODO: pets take 25% and owner gets 75%
					long xpReward = (long)(playerExpValue * damagePercent); // exp for damage percent

					long expCap = (long)(living.ExperienceValue * ServerProperties.Properties.XP_PVP_CAP_PERCENT / 100);
					if (xpReward > expCap)
						xpReward = expCap;

					//outpost XP
					//1.54 http://www.camelotherald.com/more/567.shtml
					//- Players now receive an exp bonus when fighting within 16,000
					//units of a keep controlled by your realm or your guild.
					//You get 20% bonus if your guild owns the keep or a 10% bonus
					//if your realm owns the keep.

					long outpostXP = 0;

					if (!BG && living is GamePlayer)
					{
						AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(living.CurrentRegionID, living, 16000);
						if (keep != null)
						{
							byte bonus = 0;
							if (keep.Guild != null && keep.Guild == (living as GamePlayer).Guild)
								bonus = 20;
							else if (GameServer.Instance.Configuration.ServerType == eGameServerType.GST_Normal &&
									 keep.Realm == living.Realm)
								bonus = 10;

							outpostXP = (xpReward / 100) * bonus;
						}
					}
					xpReward += outpostXP;

					living.GainExperience(GameLiving.eXPSource.Player, xpReward);

					// gold
					if (living is GamePlayer)
					{
						long money = (long)(playerMoneyValue * damagePercent);
						GamePlayer player = living as GamePlayer;
						if (player.GetSpellLine("Spymaster") != null)
						{
							money += 20 * money / 100;
						}
						//long money = (long)(Money.GetMoney(0, 0, 17, 85, 0) * damagePercent * killedPlayer.Level / 50);
						player.AddMoney(money, "You recieve {0}");
						InventoryLogging.LogInventoryAction(killer, player, eInventoryActionType.Other, money);
					}

					if (killedPlayer.ReleaseType != GamePlayer.eReleaseType.Duel && expGainPlayer != null)
					{
						switch (killedPlayer.Realm)
						{
							case eRealm.Albion:
								expGainPlayer.KillsAlbionPlayers++;
								if (expGainPlayer == killer)
								{
									expGainPlayer.KillsAlbionDeathBlows++;
									if (de.Value == totalDamage)
										expGainPlayer.KillsAlbionSolo++;
								}
								break;

							case eRealm.Hibernia:
								expGainPlayer.KillsHiberniaPlayers++;
								if (expGainPlayer == killer)
								{
									expGainPlayer.KillsHiberniaDeathBlows++;
									if (de.Value == totalDamage)
										expGainPlayer.KillsHiberniaSolo++;
								}
								break;

							case eRealm.Midgard:
								expGainPlayer.KillsMidgardPlayers++;
								if (expGainPlayer == killer)
								{
									expGainPlayer.KillsMidgardDeathBlows++;
									if (de.Value == totalDamage)
										expGainPlayer.KillsMidgardSolo++;
								}
								break;
						}
						killedPlayer.DeathsPvP++;
					}
				}

				if (Properties.LOG_PVP_KILLS && playerKillers.Count > 0)
				{
					try
					{
						foreach (var pair in playerKillers)
						{

							var killLog = new PvPKillsLog();
							killLog.KilledIP = killedPlayer.Client.TcpEndpointAddress;
							killLog.KilledName = killedPlayer.Name;
							killLog.KilledRealm = GlobalConstants.RealmToName(killedPlayer.Realm);
							killLog.KillerIP = pair.Key.Client.TcpEndpointAddress;
							killLog.KillerName = pair.Key.Name;
							killLog.KillerRealm = GlobalConstants.RealmToName(pair.Key.Realm);
							killLog.RPReward = pair.Value;
							killLog.RegionName = killedPlayer.CurrentRegion.Description;
							killLog.IsInstance = killedPlayer.CurrentRegion.IsInstance;

							if (killedPlayer.Client.TcpEndpointAddress == pair.Key.Client.TcpEndpointAddress)
								killLog.SameIP = 1;

							GameServer.Database.AddObject(killLog);
						}
					}
					catch (Exception ex)
					{
						log.Error(ex);
					}
				}
			}
		}

		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
	}
}
