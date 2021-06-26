using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Behaviour;

namespace DOL.GS.Quests
{
	public class StopGoal : DataQuestJsonGoal
	{
		private readonly List<int> m_stopGoals = new List<int>();

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override bool Visible => false;

		public StopGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			foreach (int id in db.StopGoals)
				m_stopGoals.Add(id);
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("StopGoals", m_stopGoals);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
		}

		public override PlayerGoalState ForceStartGoal(PlayerQuest questData)
		{
			var state = base.ForceStartGoal(questData);
			new RegionTimer(questData.QuestPlayer, _timer =>
			{
				foreach (var stopId in m_stopGoals)
				{
					var goalState = questData.GoalStates.Find(gs => gs.GoalId == stopId);
					if (goalState == null)
					{
						questData.GoalStates.Add(new PlayerGoalState
						{
							GoalId = stopId,
							Progress = 0,
							State = eQuestGoalStatus.Aborted,
						});
					}
					else if (!goalState.IsFinished)
						goalState.State = eQuestGoalStatus.Aborted;
				}
				EndGoal(questData, state);
				return 0;
			}).Start(1);
			return state;
		}
	}
}
