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
    }
}
