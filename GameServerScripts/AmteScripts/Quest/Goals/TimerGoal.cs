using DOL.Events;
using System;
using System.Collections.Generic;
using DOL.GS.Utils;

namespace DOL.GS.Quests
{
	public class TimerGoal : DataQuestJsonGoal
	{
		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override bool Visible => false;

		private readonly int m_seconds;

		public TimerGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_seconds = db.Seconds;
			m_seconds = FastMath.Clamp(m_seconds, 1, int.MaxValue); // minimum 1 sec
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("Seconds", m_seconds);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			if (e == GamePlayerEvent.GameEntered && sender == questData.QuestPlayer)
				StartTimer(questData, goalData);
		}

		public override PlayerGoalState ForceStartGoal(PlayerQuest questData)
		{
			var state = base.ForceStartGoal(questData);
			StartTimer(questData, state);
			return state;
		}

		private void StartTimer(PlayerQuest questData, PlayerGoalState goalData)
		{
			var tempKey = $"QUEST_TIMER_{Quest.Id}-{GoalId}";
			if (questData.QuestPlayer.TempProperties.getProperty<RegionTimer>(tempKey) != null)
				return;
			if (m_seconds <= 600)
				questData.QuestPlayer.Out.SendTimerWindow(Description, m_seconds);
			var timer = new RegionTimer(questData.QuestPlayer, _timer =>
				{
					AdvanceGoal(questData, goalData);
					return 0;
				});
			timer.Start(m_seconds * 1000);
			questData.QuestPlayer.TempProperties.setProperty(tempKey, timer);
		}
	}
}
