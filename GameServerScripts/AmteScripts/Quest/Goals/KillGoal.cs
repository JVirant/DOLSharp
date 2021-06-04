using DOL.Events;
using System;
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

		public KillGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId)
		{
			m_description = db.Description;
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName, (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			if (m_target == null)
				throw new Exception($"[DataQuestJson] Quest {quest.Id}: can't load the goal id {goalId}, the target npc (name: {db.TargetName}, reg: {db.TargetRegion}) is not found");
			m_killCount = db.KillCount;
		}

		public override object GetDatabaseJsonObject()
		{
			return new
			{
				Description = m_description,
				TargetName = m_target.Name,
				TargetRegion = m_target.CurrentRegionID,
				KillCount = m_killCount,
			};
		}

		public override void Notify(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			// Enemy of player with quest was killed, check quests and steps
			if (e == GameLivingEvent.EnemyKilled && args is EnemyKilledEventArgs killedArgs)
			{
				var killed = killedArgs.Target;
				if (killed == null || m_target.Name != killed.Name || m_target.CurrentRegion != killed.CurrentRegion)
					return;
				goalData.Progress += 1;
				if (goalData.Progress >= ProgressTotal)
					EndGoal(questData, goalData);
				questData.SaveIntoDatabase();
				questData.QuestPlayer.Out.SendQuestUpdate(questData);
			}
		}
	}
}
