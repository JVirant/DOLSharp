using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.GS.Commands;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.Scripts
{
	[Cmd("&itempower",
		ePrivLevel.GM,
		"Donne des informations sur la puissance des items",
		"'/itempower [slot]' Affiche les informations de l'item")]
	public class ItempowerCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			int slot;
			if (args.Length == 1)
				slot = (int)eInventorySlot.LastBackpack;
			else if (!int.TryParse(args[1], out slot))
			{
				DisplaySyntax(client);
				return;
			}

			InventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)slot);
			if (item == null)
			{
				ChatUtil.SendSystemMessage(client, "Le slot " + slot + " est vide.");
				return;
			}

			int gemPoints = 0;
			int gemMax = 0;
			var delve = new List<string>
							{
								"          Nom: " + item.Name,
								"       Niveau: " + item.Level,
								"      Qualité: " + item.Quality + "%",
								" "
							};
			gemPoints += GetGemImbuePoints(delve, item.Bonus1Type, item.Bonus1, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus2Type, item.Bonus2, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus3Type, item.Bonus3, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus4Type, item.Bonus4, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus5Type, item.Bonus5, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus6Type, item.Bonus6, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus7Type, item.Bonus7, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus8Type, item.Bonus8, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus9Type, item.Bonus9, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.Bonus10Type, item.Bonus10, ref gemMax);
			gemPoints += GetGemImbuePoints(delve, item.ExtraBonusType, item.ExtraBonus, ref gemMax);
			gemPoints = (gemPoints + gemMax) / 2;
			delve.Add(" ");
			delve.Add("        Total: " + gemPoints + "/" + GetItemMaxImbuePoints(item.Template));
			delve.Add("Info:");
			delve.Add("Un nombre supérieur à 100 indique la présence de bonus Toa (ou beaucoup trop de bonus).");
			delve.Add("Un nombre supérieur à 1 000 000 indique la présence de bonus interdit.");
			client.Out.SendCustomTextWindow("Item power", delve);
		}

		protected static int GetGemImbuePoints(List<string> delve, int bonusType, int bonusValue, ref int max)
		{
			if (bonusType == 0 || bonusValue == 0)
				return 0;
			int gemBonus;
			if (bonusType <= (int)eProperty.Stat_Last || bonusType == (int)eProperty.Acuity) //stat
				gemBonus = (int)(((bonusValue - 1) * 2 / 3) + 1);
			else if (bonusType == (int)eProperty.MaxMana) //mana
				gemBonus = (int)((bonusValue * 2) - 2);
			else if (bonusType == (int)eProperty.MaxHealth) //HP
				gemBonus = (int)(bonusValue / 4);
			else if (bonusType <= (int)eProperty.Resist_Last) //resist
				gemBonus = (int)((bonusValue * 2) - 2);
			else if (bonusType <= (int)eProperty.Skill_Last) //skill
				gemBonus = (int)((bonusValue - 1) * 5);
			else if (bonusType >= (int)eProperty.ToABonus_First && bonusType <= (int)eProperty.ToABonus_Last) // Toa
				gemBonus = bonusValue * 100;
			else if (bonusType == (int)eProperty.PowerPoolCapBonus) //mana cap
				gemBonus = (int)((bonusValue * 2) - 2) * 4;
			else if (bonusType == (int)eProperty.MaxHealthCapBonus) //HP cap
				gemBonus = (int)(bonusValue / 4) * 4;
			else if (bonusType >= (int)eProperty.StatCapBonus_First && bonusType <= (int)eProperty.StatCapBonus_Last) //stat cap
				gemBonus = (int)(((bonusValue - 1) * 2 / 3) + 1) * 4;
			else if (bonusType >= (int)eProperty.ResCapBonus_First && bonusType <= (int)eProperty.ResCapBonus_Last) //resist cap
				gemBonus = (int)((bonusValue * 2) - 2) * 4;
			else if (bonusType == (int)eProperty.MaxSpeed ||
				bonusType == (int)eProperty.MaxConcentration ||
				bonusType == (int)eProperty.ArmorFactor ||
				bonusType == (int)eProperty.ArmorAbsorption ||
				bonusType == (int)eProperty.HealthRegenerationRate ||
				bonusType == (int)eProperty.PowerRegenerationRate ||
				bonusType == (int)eProperty.EnduranceRegenerationRate ||
				bonusType == (int)eProperty.SpellRange ||
				bonusType == (int)eProperty.ArcheryRange ||
				bonusType == (int)eProperty.MeleeSpeed ||
				bonusType == (int)eProperty.LivingEffectiveLevel ||
				bonusType == (int)eProperty.EvadeChance ||
				bonusType == (int)eProperty.BlockChance ||
				bonusType == (int)eProperty.ParryChance ||
				bonusType == (int)eProperty.FatigueConsumption ||
				bonusType == (int)eProperty.MeleeDamage ||
				bonusType == (int)eProperty.RangedDamage ||
				bonusType == (int)eProperty.FumbleChance ||
				bonusType == (int)eProperty.MesmerizeDurationReduction ||
				bonusType == (int)eProperty.StunDurationReduction ||
				bonusType == (int)eProperty.SpeedDecreaseDurationReduction ||
				bonusType == (int)eProperty.BladeturnReinforcement ||
				bonusType == (int)eProperty.DefensiveBonus ||
				bonusType == (int)eProperty.SpellFumbleChance ||
				bonusType == (int)eProperty.NegativeReduction ||
				bonusType == (int)eProperty.PieceAblative ||
				bonusType == (int)eProperty.ReactionaryStyleDamage ||
				bonusType == (int)eProperty.SpellPowerCost ||
				bonusType == (int)eProperty.StyleCostReduction ||
				bonusType == (int)eProperty.ToHitBonus ||
				bonusType == (int)eProperty.AllSkills ||
				bonusType == (int)eProperty.WeaponSkill ||
				bonusType == (int)eProperty.CriticalMeleeHitChance ||
				bonusType == (int)eProperty.CriticalArcheryHitChance ||
				bonusType == (int)eProperty.CriticalSpellHitChance ||
				bonusType == (int)eProperty.WaterSpeed ||
				bonusType == (int)eProperty.SpellLevel ||
				bonusType == (int)eProperty.MissHit ||
				bonusType == (int)eProperty.KeepDamage ||
				bonusType == (int)eProperty.DPS ||
				bonusType == (int)eProperty.MagicAbsorption ||
				bonusType == (int)eProperty.CriticalHealHitChance ||
				bonusType == (int)eProperty.BountyPoints ||
				bonusType == (int)eProperty.XpPoints ||
				bonusType == (int)eProperty.Resist_Natural ||
				bonusType == (int)eProperty.ExtraHP ||
				bonusType == (int)eProperty.Conversion ||
				bonusType == (int)eProperty.StyleAbsorb ||
				bonusType == (int)eProperty.RealmPoints ||
				bonusType == (int)eProperty.ArcaneSyphon ||
				bonusType == (int)eProperty.MaxProperty)
				gemBonus = 1000000;
			else
				gemBonus = 1;// focus


			if (gemBonus < 1) gemBonus = 1;

			bool isPercent = ((bonusType == (int)eProperty.PowerPool)
						 || (bonusType >= (int)eProperty.Resist_First && bonusType <= (int)eProperty.Resist_Last)
						 || (bonusType >= (int)eProperty.ResCapBonus_First && bonusType <= (int)eProperty.ResCapBonus_Last)
						 || bonusType == (int)eProperty.Conversion
						 || bonusType == (int)eProperty.ExtraHP
						 || bonusType == (int)eProperty.RealmPoints
						 || bonusType == (int)eProperty.StyleAbsorb
						 || bonusType == (int)eProperty.ArcaneSyphon
						 || bonusType == (int)eProperty.BountyPoints
						 || bonusType == (int)eProperty.XpPoints);
			string ptsOrPercent = isPercent ?
				((bonusType == (int)eProperty.PowerPool) ? LanguageMgr.GetTranslation(DOL.GS.ServerProperties.Properties.SERV_LANGUAGE, "DetailDisplayHandler.WriteBonusLine.PowerPool") : "%")
				: LanguageMgr.GetTranslation(DOL.GS.ServerProperties.Properties.SERV_LANGUAGE, "DetailDisplayHandler.WriteBonusLine.Points");

			delve.Add(string.Format(
						"- {0}: {1}{2} - power = {3}",
						SkillBase.GetPropertyName((eProperty)bonusType),
						bonusValue.ToString("+0 ;-0 ;0 "),
						ptsOrPercent,
						gemBonus
					));

			if (max < gemBonus)
				max = gemBonus;
			return gemBonus;
		}

		protected static int GetItemMaxImbuePoints(ItemTemplate item)
		{
			if (item.Level > 51)
				return 32;
			if (item.Level < 1)
				return 0;
			return itemMaxBonusLevel[item.Level - 1, item.Quality.Clamp(94, 100) - 94];
		}

		private static readonly int[,] itemMaxBonusLevel =  // taken from mythic Spellcraft calculator
		{
			{0,1,1,1,1,1,1},
			{1,1,1,1,1,2,2},
			{1,1,1,2,2,2,2},
			{1,1,2,2,2,3,3},
			{1,2,2,2,3,3,4},
			{1,2,2,3,3,4,4},
			{2,2,3,3,4,4,5},
			{2,3,3,4,4,5,5},
			{2,3,3,4,5,5,6},
			{2,3,4,4,5,6,7},
			{2,3,4,5,6,6,7},
			{3,4,4,5,6,7,8},
			{3,4,5,6,6,7,9},
			{3,4,5,6,7,8,9},
			{3,4,5,6,7,8,10},
			{3,5,6,7,8,9,10},
			{4,5,6,7,8,10,11},
			{4,5,6,8,9,10,12},
			{4,6,7,8,9,11,12},
			{4,6,7,8,10,11,13},
			{4,6,7,9,10,12,13},
			{5,6,8,9,11,12,14},
			{5,7,8,10,11,13,15},
			{5,7,9,10,12,13,15},
			{5,7,9,10,12,14,16},
			{5,8,9,11,12,14,16},
			{6,8,10,11,13,15,17},
			{6,8,10,12,13,15,18},
			{6,8,10,12,14,16,18},
			{6,9,11,12,14,16,19},
			{6,9,11,13,15,17,20},
			{7,9,11,13,15,17,20},
			{7,10,12,14,16,18,21},
			{7,10,12,14,16,19,21},
			{7,10,12,14,17,19,22},
			{7,10,13,15,17,20,23},
			{8,11,13,15,17,20,23},
			{8,11,13,16,18,21,24},
			{8,11,14,16,18,21,24},
			{8,11,14,16,19,22,25},
			{8,12,14,17,19,22,26},
			{9,12,15,17,20,23,26},
			{9,12,15,18,20,23,27},
			{9,13,15,18,21,24,27},
			{9,13,16,18,21,24,28},
			{9,13,16,19,22,25,29},
			{10,13,16,19,22,25,29},
			{10,14,17,20,23,26,30},
			{10,14,17,20,23,27,31},
			{10,14,17,20,23,27,31},
			{10,15,18,21,24,28,32},
		};
	}
}
