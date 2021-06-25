using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class KilledGoal : DataQuestJsonGoal
	{
		private GameNPC m_target;
		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override QuestZonePoint PointA => new QuestZonePoint(m_target);

		public KilledGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName, (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			if (m_target == null)
				throw new Exception($"[DataQuestJson] Quest {quest.Id}: can't load the goal id {goalId}, the target npc (name: {db.TargetName}, reg: {db.TargetRegion}) is not found");
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("TargetName", m_target.Name);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			// The player is dying, check quests and steps
			if (e == GameLivingEvent.Dying && args is DyingEventArgs dyingEventArgs && sender == questData.QuestPlayer)
			{
				var killer = dyingEventArgs.Killer;
				if (killer == null || m_target.Name != killer.Name || m_target.CurrentRegion != killer.CurrentRegion)
					return;
				AdvanceGoal(questData, goalData);
			}
		}
	}
}
