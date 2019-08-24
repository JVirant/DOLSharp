/************************************************************************************
 *	Utilisation:                                                                    *
 *	- Id_nb:                                                                        *
 *		Doit commencer par "chope" (/item savetemplate chopeXXXX)                   *
 *	- mbonus:                                                                       *
 *		Règle les bonus/malus de la bière                                           *
 *		(/item mbonus <id sauf 0> <property> <value>)                               *
 *	- Extra bonus:                                                                  *
 *		Règle la vitesse du personnage (/item mbonus 0 145 <value>)                 *
 *		ATTENTION: c'est un pourcentage par rapport à la vitesse de base du perso.  *
 *	- Durability:                                                                   *
 *		Règle le temps des effets en secondes (/item durability <value> <maxValue>) *
 *	- Charges:                                                                      *
 *		Règle le nombre de fois que l'on peut boire (/item charges <value>)         *
 ************************************************************************************/

/***********************************
 *	©2006 - Dre - www.Amtenael.com *
 *	Version 1.0 - BeerItem.cs      *
 ***********************************/

using System;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class BeerEvent
	{
		[GameServerStartedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseSlot));
		}

		protected static void PlayerUseSlot(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;
			if (player == null) return;

			UseSlotEventArgs uArgs = (UseSlotEventArgs)args;
			InventoryItem item = player.Inventory.GetItem((eInventorySlot)uArgs.Slot);
			
			if (item != null && (item.Id_nb.ToLower().StartsWith("chope")
				|| item.Id_nb.ToLower().StartsWith("boisson") || item.Id_nb.ToLower().StartsWith("alcool")))
			{
				if(player.TempProperties.getProperty("Drink", false))
				{
					player.Out.SendMessage("Vous êtes déjà en train de boire !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				if(item.Charges < 0)
					return;

				bool Alcool = false;
				if (item.Id_nb.ToLower().StartsWith("chope") || item.Id_nb.ToLower().StartsWith("alcool"))
					Alcool = true;

				//Emote
				if(item.SlotPosition != (int)eInventorySlot.RightHandWeapon)
					player.Inventory.MoveItem((eInventorySlot)item.SlotPosition, eInventorySlot.RightHandWeapon, 1);
				player.Emote(eEmote.Drink);
				foreach(GamePlayer target in player.GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
					target.Out.SendMessage(GameServer.ServerRules.GetPlayerName(target, player) + " boit " + item.Name + ".", eChatType.CT_Emote, eChatLoc.CL_SystemWindow);

				#region Bonus/Malus
				bool effect = false;
				if(item.Bonus1Type > 0 && item.Bonus1 != 0)
				{
					player.BuffBonusCategory4[item.Bonus1Type] += item.Bonus1;
					effect = true;
				}
				if(item.Bonus2Type > 0 && item.Bonus2 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus2Type] += item.Bonus2;
					effect = true;
				}
				if(item.Bonus3Type > 0 && item.Bonus3 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus3Type] += item.Bonus3;
					effect = true;
				}
				if(item.Bonus4Type > 0 && item.Bonus4 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus4Type] += item.Bonus4;
					effect = true;
				}
				if(item.Bonus5Type > 0 && item.Bonus5 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus5Type] += item.Bonus5;
					effect = true;
				}
				if (item.Bonus6Type > 0 && item.Bonus6 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus6Type] += item.Bonus6;
					effect = true;
				}
				if (item.Bonus7Type > 0 && item.Bonus7 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus7Type] += item.Bonus7;
					effect = true;
				}
				if (item.Bonus8Type > 0 && item.Bonus8 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus8Type] += item.Bonus8;
					effect = true;
				}
				if (item.Bonus9Type > 0 && item.Bonus9 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus9Type] += item.Bonus9;
					effect = true;
				}
				if (item.Bonus10Type > 0 && item.Bonus10 != 0)
				{
                    player.BuffBonusCategory4[item.Bonus10Type] += item.Bonus10;
					effect = true;
				}
				if(item.ExtraBonusType > 0 && item.ExtraBonus != 0)
				{
					if(item.ExtraBonus < 0)
						player.BuffBonusMultCategory1.Set(item.ExtraBonusType, "chope", -item.ExtraBonus/100.0);
					else
						player.BuffBonusMultCategory1.Set(item.ExtraBonusType, "chope", item.ExtraBonus/100.0);
					effect = true;
				}
				#endregion

				if (effect)
				{
					//Timer de fin des effets
					RegionTimer EffectTimer = new RegionTimer(player, EffectCallback);
					EffectTimer.Properties.setProperty("Chope", item);

					if(item.Durability > 0)
						EffectTimer.Start(item.Durability*1000);
					else
						EffectTimer.Start(60000 * 5); //5min
					
					SendUpdates(player);
				}

				RegionTimer drinkTimer = new RegionTimer(player, DrinkCallback);
				drinkTimer.Properties.setProperty("Chope", item);
				drinkTimer.Start(6000);
				player.TempProperties.setProperty("Drink", true);

				//Timer des emotes/phrases aléatoire
				if (!Alcool)
					return;
                int RemainTime = player.TempProperties.getProperty("ChopeMaxTime", 0) - player.TempProperties.getProperty("Chopetime", 0);
				if(RemainTime < item.Durability*1000)
				{
					player.TempProperties.setProperty("ChopeMaxTime", item.Durability*1000);
					player.TempProperties.removeProperty("Chopetime");
				}
				else
				{
					RegionTimer EmotesTimer = new RegionTimer(player, EmotesCallback, 5000);
					EmotesTimer.Start(5000);
				}
			}
		}

		private static int DrinkCallback(RegionTimer timer)
		{
            if (timer.Properties.getProperty<InventoryItem>("Chope", null) == null ||
                !(timer.Owner is GamePlayer))
                return 0;

            InventoryItem item = timer.Properties.getProperty<InventoryItem>("Chope", null);
			GamePlayer player = (GamePlayer)timer.Owner;

			//Charge (nombre de fois qu'on peut boire)
			item.Charges--;

            if (item.Charges <= 0)
            {
                ((GamePlayerInventory)player.Inventory).RemoveCountFromStack(item, 1);
                InventoryLogging.LogInventoryAction(player, "(drink)", eInventoryActionType.Other, item.Template);
            }
            else
            {
                GameServer.Database.SaveObject(item);
                player.Out.SendInventoryItemsUpdate(new[] { item });
            }
			player.TempProperties.removeProperty("Drink");

            if (item.SpellID > 0)
                foreach (GamePlayer pl1 in player.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
                    pl1.Out.SendSpellEffectAnimation(player, player, (ushort)item.SpellID, 0, false, 1);

			return 0;
		}

		/// <summary>
		/// Phrase/Emote aléatoire
		/// </summary>
		private static int EmotesCallback(RegionTimer timer)
		{
            if (!(timer.Owner is GameLiving))
                return 0;
			GameLiving living = (GameLiving)timer.Owner;
            int time = living.TempProperties.getProperty("Chopetime", 5000);

            if (time > living.TempProperties.getProperty("ChopeMaxTime", 0))
			{
				living.TempProperties.removeProperty("Chopetime");
				living.TempProperties.removeProperty("ChopeMaxTime");
				return 0;
			}

		    living.TempProperties.setProperty("Chopetime", time + 5000);
		    switch(Util.Random(1, 10))
			{
				case 1: living.Say("Burp"); break;
				case 2: living.Emote(eEmote.Laugh); break;
				case 3: living.Emote(eEmote.Stagger); break;
				case 4: living.Emote(eEmote.Victory); break;
				case 5: living.Emote(eEmote.Rofl); break;
				case 6: living.Emote(eEmote.Mememe); break;
				default: break;
			}
			return 5000;
		}

		/// <summary>
		/// Fin des effets
		/// </summary>
		private static int EffectCallback(RegionTimer timer)
		{
            if (timer.Properties.getProperty<InventoryItem>("Chope", null) == null ||
                !(timer.Owner is GameLiving))
				return 0;

            InventoryItem item = timer.Properties.getProperty<InventoryItem>("Chope", null);
            GameLiving gameLiving = (GameLiving)timer.Owner;

			if(item.Bonus1Type > 0 && item.Bonus1 != 0)
				gameLiving.BuffBonusCategory4[item.Bonus1Type] -= item.Bonus1;
			if(item.Bonus2Type > 0 && item.Bonus2 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus2Type] -= item.Bonus2;
			if(item.Bonus3Type > 0 && item.Bonus3 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus3Type] -= item.Bonus3;
			if(item.Bonus4Type > 0 && item.Bonus4 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus4Type] -= item.Bonus4;
			if(item.Bonus5Type > 0 && item.Bonus5 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus5Type] -= item.Bonus5;
			if (item.Bonus6Type > 0 && item.Bonus6 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus6Type] -= item.Bonus6;
			if (item.Bonus7Type > 0 && item.Bonus7 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus7Type] -= item.Bonus7;
			if (item.Bonus8Type > 0 && item.Bonus8 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus8Type] -= item.Bonus8;
			if (item.Bonus9Type > 0 && item.Bonus9 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus9Type] -= item.Bonus9;
			if (item.Bonus10Type > 0 && item.Bonus10 != 0)
                gameLiving.BuffBonusCategory4[item.Bonus10Type] -= item.Bonus10;
			if(item.ExtraBonusType > 0 && item.ExtraBonus != 0)
				gameLiving.BuffBonusMultCategory1.Remove(item.ExtraBonusType, "chope");

			SendUpdates(gameLiving);
			
			return 0;
		}
		
		/// <summary>
		/// Envoit les packets d'update
		/// </summary>
		private static void SendUpdates(GameLiving living)
		{
            if (living is GamePlayer)
            {
                GamePlayer player = (GamePlayer) living;
                player.Out.SendUpdateMaxSpeed();
                player.Out.SendCharStatsUpdate();
                player.Out.SendUpdateWeaponAndArmorStats();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
            }


		    if (living.Health < living.MaxHealth) living.StartHealthRegeneration();
			else if (living.Health > living.MaxHealth) living.Health = living.MaxHealth;

			if (living.Mana < living.MaxMana) living.StartPowerRegeneration();
			else if (living.Mana > living.MaxMana) living.Mana = living.MaxMana;

			if (living.Endurance < living.MaxEndurance) living.StartEnduranceRegeneration();
			else if (living.Endurance > living.MaxEndurance) living.Endurance = living.MaxEndurance;
		}
	}
}
