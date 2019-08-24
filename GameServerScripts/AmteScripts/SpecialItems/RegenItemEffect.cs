using System;
using System.Collections.Generic;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class RegenItemEffect : StaticEffect
	{
		private RegionTimer m_tickTimer;

		public override string Name
		{
			get { return "Chaleur du feu de camp"; }
		}
		public override int RemainingTime { get { return 1; } }

		public override ushort Icon { get { return 14800; } }
		//public override bool IsSpellIcon { get { return true; } }

		public override void Cancel(bool playerCancel)
		{
			if (playerCancel && Owner is GamePlayer)
				((GamePlayer)Owner).Out.SendMessage("Ne restez pas assis près d'un feu de camp pour retirer cet effet.",
													 eChatType.CT_System, eChatLoc.CL_SystemWindow);
			else
				Stop();
		}

		public override IList<string> DelveInfo
		{
			get
			{
				List<string> text = new List<string>
				                    {
				                    		"La chaleur du feu de camp permet de vous reposer plus rapidement."
				                    };
				return text;
			}
		}

		public override void Start(GameLiving target)
		{
			if (target == null) return;
			m_owner = target;
			target.EffectList.Add(this);
			if (m_tickTimer != null)
			{
				m_tickTimer.Stop();
				m_tickTimer = null;
			}
			m_tickTimer = new RegionTimer(target)
			              {
			              		Callback = PulseCallback
			              };
			m_tickTimer.Start(5000);
			m_owner.BuffBonusCategory4[(int) eProperty.HealthRegenerationRate] += m_owner.Level;
			m_owner.BuffBonusCategory4[(int)eProperty.EnduranceRegenerationRate] += 5;
			m_owner.BuffBonusCategory4[(int)eProperty.PowerRegenerationRate] += (m_owner.Level + 2) / 2;
			m_owner.TempProperties.setProperty("regenItem_level", (int)m_owner.Level);
		}

		private int PulseCallback(RegionTimer callingtimer)
		{
			if (!_FireIsHere(m_owner))
				Stop();
			return 5000;
		}

		public override void Stop()
		{
			if (m_owner == null) return;
			if (m_tickTimer != null)
			{
				m_tickTimer.Stop();
				m_tickTimer = null;
			}
			m_owner.EffectList.Remove(this);
			int level = m_owner.TempProperties.getProperty("regenItem_level", m_owner.Level);
			m_owner.BuffBonusCategory4[(int)eProperty.HealthRegenerationRate] -= level;
			m_owner.BuffBonusCategory4[(int)eProperty.EnduranceRegenerationRate] -= 5;
			m_owner.BuffBonusCategory4[(int)eProperty.PowerRegenerationRate] -= (level + 2) / 2;
		}

		[GameServerStartedEvent]
		public static void GSStart(DOLEvent e, object sender, EventArgs args)
		{
			// TODO: Create events GamePlayerEvent.Sit and GamePlayerEvent.StandUp
			//GameEventMgr.AddHandler(GamePlayerEvent.Sit, new DOLEventHandler(_OnPlayerSit));
			//GameEventMgr.AddHandler(GamePlayerEvent.StandUp, new DOLEventHandler(_OnRemoveEffectEvent));
		}

		[GameServerStoppedEvent]
		public static void GSStop(DOLEvent e, object sender, EventArgs args)
		{
			
		}

		private static void _OnPlayerSit(DOLEvent e, object sender, EventArgs arguments)
		{
			GamePlayer player = (GamePlayer)sender;
			if (!_FireIsHere(player))
				return;

			RegenItemEffect eff = ((GamePlayer)sender).EffectList.GetOfType<RegenItemEffect>();
			if (eff != null)
				eff.Stop();
			eff = new RegenItemEffect();
			eff.Start(player);
		}

		private static void _OnRemoveEffectEvent(DOLEvent e, object sender, EventArgs arguments)
		{
			RegenItemEffect eff = ((GamePlayer)sender).EffectList.GetOfType<RegenItemEffect>();
			if (eff != null)
				eff.Stop();
		}

		private static bool _FireIsHere(GameObject player)
		{
			foreach (object obj in player.GetItemsInRadius(WorldMgr.SAY_DISTANCE))
				if (obj is GameInventoryItem && ((GameInventoryItem)obj).Id_nb.StartsWith("regen_"))
					return true;
			return false;
		}
	}
}
