using DOL.Events;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class InteractGoal : DataQuestJsonGoal
	{
		private GameNPC m_target;
		private string m_text;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override QuestZonePoint PointA => new QuestZonePoint(m_target);

		public InteractGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ??  "", (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			if (m_target == null)
				m_target = quest.Npc;
			m_text = db.Text;
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("TargetName", m_target.Name);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			dict.Add("Text", m_text);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			var player = questData.QuestPlayer;
			if (e == GameObjectEvent.InteractWith && args is InteractWithEventArgs interact && interact.Target.Name == m_target.Name && interact.Target.CurrentRegion == m_target.CurrentRegion)
			{
				ChatUtil.SendPopup(player, BehaviourUtils.GetPersonalizedMessage(m_text, player));
				AdvanceGoal(questData, goalData);
			}
		}
	}
}
