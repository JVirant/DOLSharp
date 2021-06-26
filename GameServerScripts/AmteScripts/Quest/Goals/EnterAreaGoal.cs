using DOL.Events;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DOL.GS.Quests
{
	public class EnterAreaGoal : DataQuestJsonGoal
	{
		private Area.Circle m_area;
		private ushort m_areaRegion;
		private QuestZonePoint m_pointA;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override QuestZonePoint PointA => m_pointA;

		public EnterAreaGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_area = new Area.Circle($"{quest.Name} EnterAreaGoal {goalId}", new Vector3((float)db.AreaCenter.X, (float)db.AreaCenter.Y, (float)db.AreaCenter.Z), (int)db.AreaRadius);
			m_area.DisplayMessage = false;
			m_areaRegion = db.AreaRegion;

			var reg = WorldMgr.GetRegion(m_areaRegion);
			reg.AddArea(m_area);
			m_area.RegisterPlayerEnter(OnPlayerEnterArea);
			m_area.RegisterPlayerLeave(OnPlayerLeaveArea);
			m_pointA = new QuestZonePoint(reg.GetZone(m_area.Position), m_area.Position);
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("AreaCenter", m_area.Position);
			dict.Add("AreaRadius", m_area.Radius);
			dict.Add("AreaRegion", m_areaRegion);
			return dict;
		}

		private void OnPlayerEnterArea(DOLEvent e, object sender, EventArgs arguments)
		{
			var args = (AreaEventArgs)arguments;
			if (!(args.GameObject is GamePlayer player))
				return;
			var questData = player.QuestList.Find(q => q is PlayerQuest pq && pq.QuestId == Quest.Id) as PlayerQuest;
			if (questData != null && IsActive(questData))
			{
				var goalData = questData.GoalStates.Find(s => s.GoalId == GoalId);
				if (goalData != null)
					AdvanceGoal(questData, goalData);
			}
		}
		private void OnPlayerLeaveArea(DOLEvent e, object sender, EventArgs arguments)
		{
			var args = (AreaEventArgs)arguments;
			if (!(args.GameObject is GamePlayer player))
				return;
			var quest = player.QuestList.Find(q => q is PlayerQuest pq && pq.QuestId == Quest.Id);
			if (quest is PlayerQuest questData && IsActive(questData))
			{
				var goalData = questData.GoalStates.Find(s => s.GoalId == GoalId);
				if (goalData == null)
					return;
				goalData.Progress -= 1;
				goalData.State = eQuestGoalStatus.Active;
				questData.SaveIntoDatabase();
				questData.QuestPlayer.Out.SendQuestUpdate(questData);
			}
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			// nothing to do, everything is in the area handler
		}

		public override void Unload()
		{
			base.Unload();
			m_area.UnRegisterPlayerEnter(OnPlayerEnterArea);
			m_area.UnRegisterPlayerLeave(OnPlayerLeaveArea);
			WorldMgr.GetRegion(m_areaRegion)?.RemoveArea(m_area);
		}
	}
}
