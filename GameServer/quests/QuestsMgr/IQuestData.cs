using DOL.Database;
using System.Collections.Generic;

namespace DOL.GS.Quests
{
	public interface IQuestData
	{
		ushort QuestId { get; }
		string Name { get; }
		string Description { get; }
		string Summary { get; }
		string Story { get; }
		string Conclusion { get; }
		int Level { get; }
		IList<IQuestGoal> Goals { get; }
		IList<IQuestGoal> VisibleGoals { get; }
		IQuestRewards FinalRewards { get; }
		eQuestStatus Status { get; }
	}

	public enum eQuestStatus
	{
		InProgress = 0,
		Done = 1,
	}

	public interface IQuestGoal
	{
		string Description { get; }
		eQuestGoalType Type { get;  }
		int Progress { get; }
		int ProgressTotal { get; }
		QuestZonePoint PointA { get; }
		QuestZonePoint PointB { get; }
		eQuestGoalStatus Status { get; }
		ItemTemplate QuestItem { get; }
	}

	public interface IQuestRewards
	{
		List<ItemTemplate> BasicItems { get; }
		List<ItemTemplate> OptionalItems { get; }
		int ChoiceOf { get; }
		long Money { get; }
		long Experience { get; }
	}

	public enum eQuestGoalType
	{
		Unknown = 0,
		Kill = 3,
		ScoutMission = 5,
	}

	public enum eQuestGoalStatus
	{
		NotStarted = -1,
		InProgress = 0,
		Done = 1,
	}

	public struct QuestZonePoint
	{
		public ushort ZoneID;
		public ushort X;
		public ushort Y;

		public static QuestZonePoint None => new QuestZonePoint { ZoneID = 0, X = 0, Y = 0 };
	}
}
