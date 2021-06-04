using DOL.Database;
using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public abstract class DataQuestJsonGoal
	{
		public readonly int GoalId;

		public virtual bool CanStartWhenOtherGoalsDone => true;
		public abstract string Description { get; }
		public abstract eQuestGoalType Type { get; }
		public abstract int ProgressTotal { get; }
		public virtual QuestZonePoint PointA => QuestZonePoint.None;
		public virtual QuestZonePoint PointB => QuestZonePoint.None;
		public virtual ItemTemplate QuestItem => null;

		public DataQuestJsonGoal(DataQuestJson quest, int goalId)
		{
			GoalId = goalId;
		}

		public void Notify(PlayerQuest questData, DOLEvent e, object sender, EventArgs args)
		{
			var goalData = questData.GoalStates.Find(gs => gs.GoalId == GoalId);
			Notify(questData, goalData, e, sender, args);
		}
		public abstract void Notify(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args);

		public virtual bool CanStart(PlayerQuest questData)
		{
			if (questData.GoalStates.Any(gs => gs.GoalId == GoalId && (gs.Active || gs.Done)))
				return false;
			// by default the previous goal should be done
			return GoalId <= 1 ? true : questData.GoalStates.Any(gs => gs.GoalId == GoalId - 1 && gs.Done);
		}
		public PlayerGoalState StartGoal(PlayerQuest questData)
		{
			if (CanStart(questData))
				return ForceStartGoal(questData);
			return null;
		}
		public virtual PlayerGoalState ForceStartGoal(PlayerQuest questData)
		{
			var goalData = new PlayerGoalState
			{
				GoalId = GoalId,
				Active = true,
			};
			questData.GoalStates.Add(goalData);
			return goalData;
		}
		public virtual void EndGoal(PlayerQuest questData, PlayerGoalState goalData)
		{
			goalData.Active = false;
			goalData.Progress = ProgressTotal;
			goalData.Done = true;

			// start other goals
			questData.Quest.Goals.Values.Foreach(g => g.StartGoal(questData));
		}

		public virtual IQuestGoal ToQuestGoal(PlayerQuest questData, PlayerGoalState goalData)
			=> new GenericDataQuestGoal(this, goalData?.Progress ?? 0, goalData == null ? eQuestGoalStatus.NotStarted : (goalData.Progress >= ProgressTotal ? eQuestGoalStatus.Done : eQuestGoalStatus.InProgress));

		public virtual object GetDatabaseJsonObject() => null;

		protected class GenericDataQuestGoal : IQuestGoal
		{
			public string Description => Goal.Description;
			public eQuestGoalType Type => Goal.Type;
			public int Progress { get; set; }
			public int ProgressTotal => Goal.ProgressTotal;
			public QuestZonePoint PointA => Goal.PointA;
			public QuestZonePoint PointB => Goal.PointB;
			public eQuestGoalStatus Status { get; set; }
			public ItemTemplate QuestItem => Goal.QuestItem;

			public readonly DataQuestJsonGoal Goal;

			public GenericDataQuestGoal(DataQuestJsonGoal goal, int progress, eQuestGoalStatus status)
			{
				Progress = progress;
				Status = status;
				Goal = goal;
			}
		}
	}
}
