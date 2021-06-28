using DOL.Database;
using DOL.Events;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class CollectGoal : DataQuestJsonGoal
	{
		private GameNPC m_target;
		private string m_text;
		private ItemTemplate m_item;
		private int m_itemCount = 1;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override QuestZonePoint PointA => new QuestZonePoint(m_target);
		public override ItemTemplate QuestItem => m_item;

		public CollectGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ??  "", (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			m_target = m_target ?? quest.Npc;
			m_text = db.Text;
			m_item = GameServer.Database.FindObjectByKey<ItemTemplate>((string)db.Item);
			m_itemCount = db.ItemCount;
			GameEventMgr.AddHandler(m_target, GameObjectEvent.ReceiveItem, _Notify);
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("TargetName", m_target.Name);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			dict.Add("Text", m_text);
			dict.Add("Item", m_item.Id_nb);
			dict.Add("ItemCount", m_itemCount);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
		}

		private void _Notify(DOLEvent e, object sender, EventArgs args)
		{
			if (e != GameObjectEvent.ReceiveItem || !(args is ReceiveItemEventArgs interact))
				return;
			if (!(interact.Source is GamePlayer player) || interact.Target != m_target)
				return;
			var (quest, goal) = DataQuestJsonMgr.FindQuestAndGoalFromPlayer(player, Quest.Id, GoalId);

			if (!player.Inventory.RemoveCountFromStack(interact.Item, m_itemCount))
			{
				ChatUtil.SendImportant(player, "An error happened, retry in a few seconds");
				return;
			}
			ChatUtil.SendPopup(player, BehaviourUtils.GetPersonalizedMessage(m_text, player));
			AdvanceGoal(quest, goal);
		}

		public override void Unload()
		{
			GameEventMgr.RemoveHandler(m_target, GameObjectEvent.ReceiveItem, _Notify);
			base.Unload();
		}
	}
}
