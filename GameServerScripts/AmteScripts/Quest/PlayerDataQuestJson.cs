using DOL.Database;
using DOL.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	/// <summary>
	/// This class hold the data about progression of a player in a specific PlayerDataQuestJson
	/// </summary>
	public class PlayerDataQuestJson : AbstractQuest, IQuestData
	{
		public ushort QuestId => Quest.Id;
		public readonly DataQuestJson Quest;
		public readonly List<PlayerGoalState> GoalStates;

		public PlayerDataQuestJson(GamePlayer owner, DBQuest dbquest) : base(owner, dbquest)
		{
			var questId = int.Parse(GetCustomProperty("QuestID"));
			GoalStates = JsonConvert.DeserializeObject<List<PlayerGoalState>>(GetCustomProperty("JsonState")) ?? new List<PlayerGoalState>();
			Quest = DataQuestJsonMgr.Quests[questId];
		}

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

		public IList<IQuestGoal> Goals => Quest.Goals.Values.Cast<IQuestGoal>().ToList();
		public IList<IQuestGoal> VisibleGoals => Quest.GetVisibleGoals(this);

		public IQuestRewards FinalRewards => new QuestRewards(Quest);

		public override bool CheckQuestQualification(GamePlayer player)
		{
			return Quest.CheckQuestQualification(this, player);
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			Quest.Notify(this, e, sender, args);
		}

		public class PlayerGoalState
		{
			public readonly int GoalId;
			public int Progress;
			public object CustomData;
			public bool IsActive = false;
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
	}
}
