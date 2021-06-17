using DOL.Events;
using System;
using System.Collections.Generic;

namespace DOL.GS.Quests
{
	public class DummyGoal : DataQuestJsonGoal
	{
		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override bool Visible => false;

		public DummyGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
		}

		public override PlayerGoalState ForceStartGoal(PlayerQuest questData)
		{
			var state = base.ForceStartGoal(questData);
			EndGoal(questData, state);
			return state;
		}
	}
}
