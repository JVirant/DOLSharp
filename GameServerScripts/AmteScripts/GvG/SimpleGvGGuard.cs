using System;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;

namespace DOL.GS.Scripts
{
	public class SimpleGvGGuard : AmteMob
	{
		public GuildCaptainGuard Captain
		{
			get
			{
				return (Brain as SimpleGvGGuardBrain)?.Captain;
			}
			set
			{
				if (Brain is SimpleGvGGuardBrain guard)
					guard.Captain = value;
			}
		}

		public override string GuildName
		{
			get => base.GuildName;
			set
			{
				var old = base.GuildName;
				base.GuildName = value;
				if (old != value)
					RefreshEmblem();
			}
		}

		public SimpleGvGGuard()
		{
			var brain = new SimpleGvGGuardBrain();
			brain.AggroLink = 3;
			SetOwnBrain(brain);
		}

		public override bool Interact(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel == 1 && !IsWithinRadius(player, InteractDistance))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Interact.TooFarAway", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
				return false;
			}
			Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
			player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));

			if (string.IsNullOrWhiteSpace(GuildName) || player.Guild == null)
				return false;
			if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
				return false;
			if (!player.GuildRank.Claim)
			{
				player.Out.SendMessage($"Bonjour {player.Name}, je ne discute pas avec les bleus, circulez.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			var cloaks = GameServer.Database.SelectObjects<NPCEquipment>("TemplateID like 'gvg_guard_%' AND Slot = 26");
			player.Out.SendMessage(
				$"Bonjour {player.Name}, vous pouvez modifier l'équippement que je porte, sélectionner l'ensemble que vous souhaitez :\n" +
				string.Join("\n", cloaks.Select(c => $"[{c.TemplateID.Substring(10)}]")),
				eChatType.CT_System,
				eChatLoc.CL_PopupWindow
			);
			return true;
		}
		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text) || string.IsNullOrWhiteSpace(GuildName))
				return false;
			if (!(source is GamePlayer player) || player.Guild == null)
				return false;
			if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
				return false;
			if (!player.GuildRank.Claim)
			{
				player.Out.SendMessage($"Bonjour {player.Name}, je ne discute pas avec les bleus, circulez.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return true;
			}

			var cloaks = GameServer.Database.SelectObjects<NPCEquipment>("TemplateID like 'gvg_guard_%' AND Slot = 26");
			text = $"gvg_guard_{text}";
			if (cloaks.Any(c => c.TemplateID == text))
				LoadEquipmentTemplateFromDatabase(text);
			RefreshEmblem();
			return true;
		}

		public override void Die(GameObject killer)
		{
			base.Die(killer);

			var plKiller = killer as GamePlayer;
			if (plKiller == null && killer is GameNPC npc)
				plKiller = npc.ControlledBrain?.GetPlayerOwner();
			if (plKiller != null && !string.IsNullOrEmpty(GuildName))
			{
				var guild = GuildMgr.GetGuildByName(GuildName);
				if (guild == null)
					return;
				var name = "un inconnu";
				if (!string.IsNullOrEmpty(plKiller.GuildName))
					name = $"un membre de la guilde {plKiller.GuildName}";
				guild.SendMessageToGuildMembers(
					$"{Captain?.Name ?? "Capitaine"}: un garde vient d'être tué par {name}.",
					eChatType.CT_Guild,
					eChatLoc.CL_ChatWindow
				);
			}
		}

		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;
			if (Captain != null)
				RefreshEmblem();
			return true;
		}

		public void RefreshEmblem()
		{
			if (string.IsNullOrWhiteSpace(GuildName) || ObjectState != eObjectState.Active || CurrentRegion == null || Inventory?.VisibleItems == null)
				return;
			var guild = GuildMgr.GetGuildByName(GuildName);
			if (guild == null)
				return;
			foreach (var item in Inventory.VisibleItems)
				if (item.Emblem != 0 || item.Color == GuildCaptainGuard.NEUTRAL_EMBLEM)
					item.Emblem = guild.Emblem;
			SaveIntoDatabase();
		}
	}
}

namespace DOL.AI.Brain
{
	public class SimpleGvGGuardBrain : AmteMobBrain
	{
		private long _lastCaptainUpdate = 0;
		private GuildCaptainGuard _captain;

		public GuildCaptainGuard Captain
		{
			get
			{
				if (_lastCaptainUpdate > DateTime.Now.Ticks)
					return _captain;
				_captain = GuildCaptainGuard.allCaptains.OrderBy(c => Body.GetDistanceTo(c)).FirstOrDefault();
				var name = _captain?.GuildName ?? "";
				if (name != Body.GuildName)
					Body.GuildName = name;
				_lastCaptainUpdate = DateTime.Now.Ticks + 60 * 1000 * 10000;
				return _captain;
			}
			set
			{
				_captain = value;
				_lastCaptainUpdate = DateTime.Now.Ticks + 60 * 1000 * 10000;
				var name = _captain?.GuildName ?? "";
				if (name != Body.GuildName)
					Body.GuildName = name;
			}
		}

		public override int AggroLevel
		{
			get { return 100; }
			set { }
		}

		protected override void CheckPlayerAggro()
		{
			if (Body.AttackState)
				return;
			foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
					continue;

				int aggro = CalculateAggroLevelToTarget(pl);
				if (aggro <= 0)
					continue;
				AddToAggroList(pl, aggro);
				if (pl.Level > Body.Level - 20 || (pl.Group != null && pl.Group.MemberCount > 2))
					BringReinforcements(pl);
			}
		}

		protected override void CheckNPCAggro()
		{
			if (Body.AttackState)
				return;
			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, Body.CurrentRegion.IsDungeon ? false : true))
			{
				if (npc.Realm != 0 || (npc.Flags & GameNPC.eFlags.PEACE) != 0 ||
					!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active ||
					npc is GameTaxi ||
					m_aggroTable.ContainsKey(npc) ||
					!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
					continue;

				int aggro = CalculateAggroLevelToTarget(npc);
				if (aggro <= 0)
					continue;
				AddToAggroList(npc, aggro);
				if (npc.Level > Body.Level)
					BringReinforcements(npc);
			}
		}

		private void BringReinforcements(GameNPC target)
		{
			int count = (int)Math.Log(target.Level - Body.Level, 2) + 1;
			foreach (GameNPC npc in Body.GetNPCsInRadius(WorldMgr.YELL_DISTANCE))
			{
				if (count <= 0)
					return;
				if (npc.Brain is SimpleGvGGuardBrain == false)
					continue;
				var brain = npc.Brain as SimpleGvGGuardBrain;
				brain.AddToAggroList(target, 1);
				brain.AttackMostWanted();
			}
		}

		public override int CalculateAggroLevelToTarget(GameLiving target)
		{
			if (target is AmtePlayer player)
			{
				if (Captain != null)
				{
					var plGuildId = player.Guild != null ? player.GuildID : "NOGUILD";
					if (target.GuildName == Body.GuildName || Captain.safeGuildIds.Contains(plGuildId))
						return 0;
					return 100;
				}
				return target.GuildName == Body.GuildName ? 0 : 100;
			}
			if (target.Realm == 0)
				return 0;
			return base.CalculateAggroLevelToTarget(target);
		}
	}
}
