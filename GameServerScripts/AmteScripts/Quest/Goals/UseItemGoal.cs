using DOL.Database;
using DOL.Events;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class UseItemGoal : DataQuestJsonGoal
	{
		private ItemTemplate m_item;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override ItemTemplate QuestItem => m_item;

		public UseItemGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_item = GameServer.Database.FindObjectByKey<ItemTemplate>((string)db.Item);
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("Item", m_item.Id_nb);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			var player = questData.QuestPlayer;
			if (e == GamePlayerEvent.UseSlot && args is UseSlotEventArgs useSlot && useSlot.Type == 0)
			{
				var usedItem = player.Inventory.GetItem((eInventorySlot)useSlot.Slot);
				if (usedItem.Id_nb == QuestItem.Id_nb)
					AdvanceGoal(questData, goalData);
			}
		}
	}
}
