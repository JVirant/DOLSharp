using AmteScripts.Managers;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;

namespace Amte
{
	public class LordRvR : GameNPC
	{
		public static int CLAIM_TIME_SECONDS = 30;
		public static int CLAIM_TIME_BETWEEN_SECONDS = 120;

		public DateTime lastClaim = new DateTime(1);

		private RegionTimer _claimTimer;

		private RegionTimer _scoreTimer;
		private Dictionary<eRealm, TimeSpan> _scores = new Dictionary<eRealm, TimeSpan>
		{
			{ eRealm.Albion, new TimeSpan(0) },
			{ eRealm.Midgard, new TimeSpan(0) },
			{ eRealm.Hibernia, new TimeSpan(0) },
		};

		public double timeBeforeClaim
		{
			get
			{
				return CLAIM_TIME_BETWEEN_SECONDS - (DateTime.Now - lastClaim).TotalSeconds;
			}
		}

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (RvrManager.Instance.IsOpen)
			{
				if (timeBeforeClaim > 0)
				{
					player.Out.SendMessage("Vous devez attendre " + Math.Round(timeBeforeClaim, 1) + " secondes avant de pouvoir pretendre au contrôle du fort.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
					return true;
				}
				player.Out.SendMessage("Voulez vous [prendre le contrôle] du fort ?", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			else
				player.Out.SendMessage("La prise de fort est momentanément indisponible.", eChatType.CT_System, eChatLoc.CL_PopupWindow);

			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text) || !(source is GamePlayer player))
				return false;
			if (text != "prendre le contrôle")
				return true;
			if (player.InCombat)
			{
				player.Out.SendMessage("Vous ne pouvez pas prendre le contrôle du fort en combat.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}
			if (timeBeforeClaim > 0)
			{
				player.Out.SendMessage("Vous devez attendre " + Math.Round(timeBeforeClaim, 1) + " secondes avant de pouvoir prétendre au contrôle du fort.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}
			if (_claimTimer != null)
			{
				player.Out.SendMessage("Je suis occupé pour le moment, revenez plus tard.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			var startTime = DateTime.Now;
			_claimTimer = new RegionTimer(
				this,
				timer => {
					if (player.InCombat)
					{
						_claimTimer = null;
						player.Out.SendCloseTimerWindow();
						player.Out.SendMessage("Vous avez été interrompu.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						return 0;
					}
					if (player.GetDistanceTo(this) > InteractDistance)
					{
						_claimTimer = null;
						player.Out.SendCloseTimerWindow();
						player.Out.SendMessage("Vous vous êtes trop éloigné.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						return 0;
					}
					var passedTime = DateTime.Now - startTime;
					if (passedTime.TotalSeconds < CLAIM_TIME_SECONDS)
						return 500;

					player.Out.SendCloseTimerWindow();
					TakeControl(player);
					_claimTimer = null;
					return 0;
				},
				500
			);
			player.Out.SendTimerWindow("Prise du fort", CLAIM_TIME_SECONDS);

			foreach (var obj in GetPlayersInRadius(ushort.MaxValue - 1))
				if (obj is GamePlayer pl)
					pl.Out.SendMessage($"{player.GuildName} commence à prendre le contrôle du fort !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			return true;
		}

		public virtual void TakeControl(GamePlayer player)
		{
			lastClaim = DateTime.Now;
			GuildName = player.GuildName;
			foreach (var obj in GetPlayersInRadius(ushort.MaxValue - 1))
				if (obj is GamePlayer pl)
					pl.Out.SendMessage($"{player.GuildName} a pris le contrôle du fort !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}

		public virtual void StartRvR()
		{
			var lastTime = DateTime.Now;
			if (_scoreTimer != null)
				_scoreTimer.Stop();
			_scoreTimer = new RegionTimer(
				this,
				timer => {
					switch(GuildName)
					{
						case "Albion": _scores[eRealm.Albion] += (DateTime.Now - lastTime); break;
						case "Midgard": _scores[eRealm.Midgard] += (DateTime.Now - lastTime); break;
						case "Hibernia": _scores[eRealm.Hibernia] += (DateTime.Now - lastTime); break;
					}
					lastTime = DateTime.Now;
					return 1000;
				},
				1000
			);
		}

		public virtual void StopRvR()
		{
			if (_scoreTimer != null)
				_scoreTimer.Stop();
			_scoreTimer = null;
		}

		public virtual string GetScores()
		{
			var str = " - Temps de détention du fort :\n";
			foreach (var kvp in _scores)
				str += GlobalConstants.RealmToName(kvp.Key) + ": " + Math.Round(kvp.Value.TotalSeconds, 1) + " secondes\n";
			return str;
		}
	}
}
