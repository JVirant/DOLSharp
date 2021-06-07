using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class KillGoal : DataQuestJsonGoal
	{
		private readonly string m_description;
		private readonly int m_killCount = 1;
		private GameNPC m_target;

		public override string Description => m_description;
		public override eQuestGoalType Type => eQuestGoalType.Kill;
		public override int ProgressTotal => m_killCount;
		public override QuestZonePoint PointA => new QuestZonePoint(m_target);

		public KillGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_description = db.Description;
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName, (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			if (m_target == null)
				throw new Exception($"[DataQuestJson] Quest {quest.Id}: can't load the goal id {goalId}, the target npc (name: {db.TargetName}, reg: {db.TargetRegion}) is not found");
			m_killCount = db.KillCount;
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("Description", m_description);
			dict.Add("TargetName", m_target.Name);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			dict.Add("KillCount", m_killCount);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			// Enemy of player with quest was killed, check quests and steps
			if (e == GameLivingEvent.EnemyKilled && args is EnemyKilledEventArgs killedArgs)
			{
				var killed = killedArgs.Target;
				if (killed == null || m_target.Name != killed.Name || m_target.CurrentRegion != killed.CurrentRegion)
					return;
				AdvanceGoal(questData, goalData);
			}
		}
	}
}
