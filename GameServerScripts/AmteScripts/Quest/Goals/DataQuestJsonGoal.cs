using DOL.Database;
using DOL.Events;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public abstract class DataQuestJsonGoal
	{
		public readonly DataQuestJson Quest;
		public readonly int GoalId;

		public string Description { get; set; }
		public abstract eQuestGoalType Type { get; }
		public abstract int ProgressTotal { get; }
		public virtual QuestZonePoint PointA => QuestZonePoint.None;
		public virtual QuestZonePoint PointB => QuestZonePoint.None;
		public virtual ItemTemplate QuestItem => null;
		public virtual bool Visible => true;
		public ItemTemplate GiveItemTemplate { get; set; }

		public List<int> StartGoalsDone { get; set; } = new List<int>();
		public List<int> EndWhenGoalsDone { get; set; } = new List<int>();

		public DataQuestJsonGoal(DataQuestJson quest, int goalId, dynamic db)
		{
			Quest = quest;
			GoalId = goalId;
			Description = db.Description;
			string item = db.GiveItem ?? "";
			if (!string.IsNullOrWhiteSpace(item))
				GiveItemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(item);
			if (db.StartGoalsDone != null)
			{
				foreach (var id in db.StartGoalsDone)
					StartGoalsDone.Add((int)id);
			}
			else if (GoalId > 1)
				StartGoalsDone.Add(GoalId - 1);
			if (db.EndWhenGoalsDone != null)
				foreach (var id in db.EndWhenGoalsDone)
					EndWhenGoalsDone.Add((int)id);
		}

		public bool IsActive(PlayerQuest questData) => questData.GoalStates.Any(gs => gs.GoalId == GoalId && gs.Active);
		public bool IsDone(PlayerQuest questData) => questData.GoalStates.Any(gs => gs.GoalId == GoalId && gs.Done);

		public void NotifyActive(PlayerQuest questData, DOLEvent e, object sender, EventArgs args)
		{
			var goalData = questData.GoalStates.Find(gs => gs.GoalId == GoalId);
			NotifyActive(questData, goalData, e, sender, args);
		}
		public abstract void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args);

		// this one is always called, useful if you want to start a goal with some hidden task
		public void Notify(PlayerQuest questData, DOLEvent e, object sender, EventArgs args)
		{
			var goalData = questData.GoalStates.Find(gs => gs.GoalId == GoalId);
			Notify(questData, goalData, e, sender, args);
		}
		// this one is always called, useful if you want to start a goal with some hidden task
		public virtual void Notify(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args) {}

		public virtual bool CanStart(PlayerQuest questData)
		{
			if (IsActive(questData) || IsDone(questData))
				return false;
			return StartGoalsDone.All(gId => questData.GoalStates.Any(gs => gs.GoalId == gId && gs.Done));
		}
		public virtual bool CanEnd(PlayerQuest questData)
		{
			return IsActive(questData) && IsDone(questData) && EndWhenGoalsDone.All(id => questData.GoalStates.Any(s => s.GoalId == id && s.Done));
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
			if (Visible)
			{
				questData.QuestPlayer.Out.SendQuestUpdate(questData);
				ChatUtil.SendScreenCenter(questData.QuestPlayer, $"{Description} - {goalData.Progress}/{ProgressTotal}");
			}
			return goalData;
		}
		public virtual void AdvanceGoal(PlayerQuest questData, PlayerGoalState goalData)
		{
			goalData.Progress += 1;
			if (goalData.Progress >= ProgressTotal)
			{
				EndGoal(questData, goalData);
				return;
			}
			questData.SaveIntoDatabase();
			if (Visible)
			{
				questData.QuestPlayer.Out.SendQuestUpdate(questData);
				ChatUtil.SendScreenCenter(questData.QuestPlayer, $"{Description} - {goalData.Progress}/{ProgressTotal}");
			}
		}
		public virtual void EndGoal(PlayerQuest questData, PlayerGoalState goalData, List<DataQuestJsonGoal> except = null)
		{
			goalData.Progress = ProgressTotal;
			goalData.Done = true;

			if (Visible)
				ChatUtil.SendScreenCenter(questData.QuestPlayer, $"{Description} - {goalData.Progress}/{ProgressTotal}");
			EndOtherGoals(questData, except ?? new List<DataQuestJsonGoal>());

			// start other goals
			if (Quest.Goals.Values.Select(g => g.StartGoal(questData)).Any(g => g != null) && CanEnd(questData))
			{
				if (GiveItemTemplate != null)
					GiveItem(questData.QuestPlayer, GiveItemTemplate);
				goalData.Active = false;
			}

			if (Visible)
				questData.QuestPlayer.Out.SendQuestUpdate(questData);
			questData.SaveIntoDatabase();
		}

		public void EndOtherGoals(PlayerQuest questData, List<DataQuestJsonGoal> except)
		{
			except.Add(this);
			foreach (var goal in Quest.Goals.Values)
				if (!except.Contains(goal) && goal.CanEnd(questData))
					goal.EndGoal(questData, questData.GoalStates.Find(s => s.GoalId == goal.GoalId), except);
		}

		public virtual IQuestGoal ToQuestGoal(PlayerQuest questData, PlayerGoalState goalData)
			=> new GenericDataQuestGoal(this, goalData?.Progress ?? 0, goalData == null || !Visible ? eQuestGoalStatus.NotStarted : (goalData.Progress >= ProgressTotal ? eQuestGoalStatus.Done : eQuestGoalStatus.InProgress));

		/// <summary>
		/// Returns the object to be saved as JSON given back as third argument in the constructor for loading
		/// </summary>
		/// <returns>A serialisable object</returns>
		public virtual Dictionary<string, object> GetDatabaseJsonObject()
		{
			return new Dictionary<string, object>
			{
				{ "Description", Description },
				{ "GiveItem", GiveItemTemplate?.Id_nb },
				{ "StartGoalsDone", StartGoalsDone.Count > 0 ? StartGoalsDone : null },
				{ "EndWhenGoalsDone", EndWhenGoalsDone.Count > 0 ? EndWhenGoalsDone : null },
			};
		}

		public virtual void Unload()
		{
			// nothing to do but we need to remove some handler sometimes
		}

		protected static void GiveItem(GamePlayer player, ItemTemplate itemTemplate)
		{
			var item = GameInventoryItem.Create(itemTemplate);
			if (!player.ReceiveItem(null, item))
			{
				player.CreateItemOnTheGround(item);
				ChatUtil.SendImportant(player, $"Your backpack is full, {itemTemplate.Name} is dropped on the ground.");
			}
		}

		public class GenericDataQuestGoal : IQuestGoal
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
