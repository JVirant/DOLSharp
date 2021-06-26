using System;
using DOL.Database;
using System.Collections.Generic;
using System.Numerics;

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

	[Flags]
	public enum eQuestGoalStatus
	{
		// Flags
		FlagActive = 0b001,
		FlagDone = 0b010,
		FlagFinished = 0b100,

		NotStarted = 0b000,
		Active = FlagActive,
		DoneAndActive = FlagDone | FlagActive,
		Completed = FlagDone | FlagFinished,
		Aborted = FlagFinished,
	}

	public struct QuestZonePoint
	{
		public ushort ZoneId;
		public ushort X;
		public ushort Y;

		public QuestZonePoint(ushort zoneId, ushort x, ushort y)
		{
			ZoneId = zoneId;
			X = x;
			Y = y;
		}

		public QuestZonePoint(GameObject obj)
		{
			ZoneId = obj.CurrentZone.ZoneSkinID;
			X = (ushort)(obj.Position.X - obj.CurrentZone.XOffset);
			Y = (ushort)(obj.Position.Y - obj.CurrentZone.YOffset);
		}

		public QuestZonePoint(Zone zone, Vector3 globalPos)
		{
			ZoneId = zone.ZoneSkinID;
			X = (ushort)(globalPos.X - zone.XOffset);
			Y = (ushort)(globalPos.Y - zone.YOffset);
		}

		public static QuestZonePoint None => new QuestZonePoint { ZoneId = 0, X = 0, Y = 0 };
	}
}
