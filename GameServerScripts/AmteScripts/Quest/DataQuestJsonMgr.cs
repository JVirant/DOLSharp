using DOL.Database;
using DOL.Events;
using DOL.GS.Behaviour;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.Quests.RewardQuest;

namespace DOL.GS.Quests
{
	public class DataQuestJsonMgr
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBDataQuestJson));
			log.Info("QuestLoader: initialized.");
		}

		[GameServerStartedEvent]
		public static void OnGameServerStarted(DOLEvent e, object sender, EventArgs args)
		{
			
			log.Info($"QuestLoader: {Quests.Count} quests loaded");
		}

		public static readonly Dictionary<int, DataQuestJson> Quests = new Dictionary<int, DataQuestJson>();
	}

	/// <summary>
	/// This class hold the "code" about this quest (requirements, steps, actions, etc)
	/// </summary>
	public class DataQuestJson
	{
		public readonly ushort Id;
		public readonly string Name;
		public readonly string Description;
		public readonly string Summary;
		public readonly string Story;
		public readonly string Conclusion;

		public readonly ushort MaxCount;
		public readonly byte MinLevel;
		public readonly byte MaxLevel;
		public readonly int[] QuestDependencyIDs;
		public readonly eCharacterClass[] AllowedClasses;

		public readonly int RewardMoney;
		public readonly int RewardXP;
		public readonly int RewardCLXP;
		public readonly int RewardRP;
		public readonly int RewardBP;
		public readonly List<ItemTemplate> OptionalRewardItemTemplates;
		public readonly List<ItemTemplate> FinalRewardItemTemplates;

		/// <summary>
		/// GoalID to DataQuestJsonGoal
		/// </summary>
		public readonly Dictionary<int, DataQuestJsonGoal> Goals;

		private DataQuestJson(DBDataQuestJson db, List<ItemTemplate> optionalItems, List<ItemTemplate> finalItems)
		{
			Id = db.Id;
			Name = db.Name;
			Description = db.Description;

			MaxCount = db.MaxCount;
			MinLevel = db.MinLevel;
			MaxLevel = db.MaxLevel;
			QuestDependencyIDs = db.QuestDependency.Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => int.Parse(id)).ToArray();
			AllowedClasses = db.AllowedClasses.Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => (eCharacterClass)int.Parse(id)).ToArray();

			RewardMoney = db.RewardMoney;
			RewardXP = db.RewardXP;
			RewardCLXP = db.RewardCLXP;
			RewardRP = db.RewardRP;
			RewardBP = db.RewardBP;

			OptionalRewardItemTemplates = optionalItems;
			FinalRewardItemTemplates = finalItems;
		}

		public static DataQuestJson Load(DBDataQuestJson db)
		{
			var optionalTemplates = db.OptionalRewardItemTemplates.Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
			var finalTemplates = db.FinalRewardItemTemplates.Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
			var items = GameServer.Database.FindObjectsByKey<ItemTemplate>(optionalTemplates.Union(finalTemplates));
			var optionalItems = optionalTemplates.Select(id => items.FirstOrDefault(it => it.Id_nb == id)).ToList();
			var finalItems = finalTemplates.Select(id => items.FirstOrDefault(it => it.Id_nb == id)).ToList();
			return new DataQuestJson(db, optionalItems, finalItems);
		}

		public List<IQuestGoal> GetVisibleGoals(PlayerDataQuestJson data)
		{
			return data.GoalStates.Where(gs => gs.IsActive).Select(gs => Goals[gs.GoalId].ToQuestGoal(data, gs)).ToList();
		}

		public bool CheckQuestQualification(PlayerDataQuestJson data, GamePlayer player)
		{
			return true;
		}

		public void Notify(PlayerDataQuestJson data, DOLEvent e, object sender, EventArgs args)
		{
			
		}
	}

	public abstract class DataQuestJsonGoal
	{
		public readonly int GoalID;

		public abstract string Description { get; }
		public abstract eQuestGoalType Type { get; }
		public abstract int ProgressTotal { get; }
		public virtual QuestZonePoint PointA => QuestZonePoint.None;
		public virtual QuestZonePoint PointB => QuestZonePoint.None;
		public virtual ItemTemplate QuestItem => null;

		public abstract PlayerDataQuestJson.PlayerGoalState StartGoal(PlayerDataQuestJson questData);
		public abstract void Notify(PlayerDataQuestJson questData, PlayerDataQuestJson.PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args);
		public abstract void EndGoal(PlayerDataQuestJson questData, PlayerDataQuestJson.PlayerGoalState goalData);

		public virtual IQuestGoal ToQuestGoal(PlayerDataQuestJson questData, PlayerDataQuestJson.PlayerGoalState goalData)
		{
			return new GenericDataQuestGoal(this, goalData.Progress, goalData.Progress >= ProgressTotal ? eQuestGoalStatus.Done : eQuestGoalStatus.InProgress);
		}

		protected class GenericDataQuestGoal : IQuestGoal
		{
			public string Description => Goal.Description;
			public eQuestGoalType Type => Goal.Type;
			public int Progress { get; set; }
			public int ProgressTotal => Goal.ProgressTotal;
			public QuestZonePoint PointA => QuestZonePoint.None;
			public QuestZonePoint PointB => QuestZonePoint.None;
			public eQuestGoalStatus Status { get; set; }
			public ItemTemplate QuestItem => Goal.QuestItem;

			public readonly DataQuestJsonGoal Goal;

			public GenericDataQuestGoal(DataQuestJsonGoal goal, int progress, eQuestGoalStatus status)
			{
				Goal = goal;
			}
		}
}

	public enum eStepStatus
	{
		Ignore = 0,
		Advance = 1,
		Finished = 2,
		Aborted = 3,
	}
}
