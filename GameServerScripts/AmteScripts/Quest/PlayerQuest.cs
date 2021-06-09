﻿using DOL.Database;
using DOL.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class PlayerGoalState
	{
		public int GoalId;
		public int Progress = 0;
		public object CustomData = null;
		public bool Active = false;
		public bool Done = false;
	}

	/// <summary>
	/// This class hold the data about progression of a player for a DataQuestJson
	/// </summary>
	public class PlayerQuest : AbstractQuest, IQuestData
	{
		private ushort m_questId;
		public ushort QuestId => m_questId;
		public readonly List<PlayerGoalState> GoalStates = new List<PlayerGoalState>();

		public DataQuestJson Quest => DataQuestJsonMgr.Quests[m_questId];
		public override string Name => Quest.Name;
		public override string Description => Quest.Description;
		public string Summary => Quest.Summary;
		public string Story => Quest.Story;
		public string Conclusion => Quest.Conclusion;
		public override int Level
		{
			get { return Quest.MinLevel; }
			set { throw new NotSupportedException("PlayerDataQuestJson set level"); }
		}

		public eQuestStatus Status => Step == -1 ? eQuestStatus.Done : eQuestStatus.InProgress;

		public IList<IQuestGoal> Goals => Quest.Goals.Values.Select(g => g.ToQuestGoal(this, GoalStates.Find(gs => gs.GoalId == g.GoalId))).ToList();
		public IList<IQuestGoal> VisibleGoals => Quest.GetVisibleGoals(this);

		public IQuestRewards FinalRewards => new QuestRewards(Quest);

		public PlayerQuest(GamePlayer owner, DataQuestJson quest) : base()
		{
			m_offerPlayer = owner;
			m_questId = quest.Id;
		}
		public PlayerQuest(GamePlayer owner, DBQuest dbquest) : base()
		{
			m_questPlayer = owner;
			m_dbQuest = dbquest;
			var json = JsonConvert.DeserializeObject<JsonState>(dbquest.CustomPropertiesString);
			m_questId = json.QuestId;
			if (json.Goals != null)
				GoalStates = json.Goals;
			else
				// start the quest
				Quest.Goals.Values.Where(g => g.CanStart(this)).Foreach(g => g.StartGoal(this));

			// shoudn't happen, we start the next goal
			if (VisibleGoals.Count == 0)
				Quest.Goals.Values.Foreach(g => g.StartGoal(this));

			if (VisibleGoals.Count == 0)
				AbortQuest();
		}

		public override bool CheckQuestQualification(GamePlayer player) => Quest.CheckQuestQualification(player);

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			Quest.Notify(this, e, sender, args);
		}

		public override void SaveIntoDatabase()
		{
			m_dbQuest.CustomPropertiesString = JsonConvert.SerializeObject(new JsonState { QuestId = QuestId, Goals = GoalStates });
			base.SaveIntoDatabase();
		}

		public class QuestRewards : IQuestRewards
		{
			public readonly DataQuestJson Quest;
			public List<ItemTemplate> BasicItems => Quest.FinalRewardItemTemplates;
			public List<ItemTemplate> OptionalItems => Quest.OptionalRewardItemTemplates;
			public int ChoiceOf => 1;
			public long Money => Quest.RewardMoney;
			public long Experience => Quest.RewardXP;

			public QuestRewards(DataQuestJson quest)
			{
				Quest = quest;
			}
		}

		internal class JsonState
		{
			public ushort QuestId;
			public List<PlayerGoalState> Goals;
		}
	}
}