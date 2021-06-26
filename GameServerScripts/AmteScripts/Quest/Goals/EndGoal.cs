using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
	public class EndGoal : DataQuestJsonGoal
	{
		private GameNPC m_target;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;

		public GameNPC Target => m_target;

		public EndGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ??  "", (ushort)(db.TargetRegion ?? 0), eRealm.None).FirstOrDefault();
			if (m_target == null)
				m_target = quest.Npc;
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("TargetName", m_target.Name);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			return dict;
		}

		public override void NotifyActive(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			var player = questData.QuestPlayer;
			
			// interact with the final NPC
			if (e == GameObjectEvent.InteractWith && args is InteractWithEventArgs interact && interact.Target.Name == m_target.Name && interact.Target.CurrentRegion == m_target.CurrentRegion)
				player.Out.SendQuestRewardWindow(interact.Target as GameNPC, player, questData);

			// receive the quest window response
			if (e == GamePlayerEvent.QuestRewardChosen && args is QuestRewardChosenEventArgs rewardArgs && rewardArgs.QuestID == questData.QuestId)
			{
				if (questData.Quest.NbChooseOptionalItems != rewardArgs.CountChosen && questData.Quest.OptionalRewardItemTemplates.Count >= questData.Quest.NbChooseOptionalItems)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "RewardQuest.Notify"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
					return;
				}

				var items = questData.Quest.OptionalRewardItemTemplates.Where((item, idx) => rewardArgs.ItemsChosen.Contains(idx + 1)).ToList();
				questData.Quest.FinishQuest(questData, items);
			}
		}

		public override PlayerGoalState ForceStartGoal(PlayerQuest questData)
		{
			var res = base.ForceStartGoal(questData);
			if (res.IsActive && GameMath.IsWithinRadius(questData.QuestPlayer, Target, WorldMgr.OBJ_UPDATE_DISTANCE))
				questData.QuestPlayer.Out.SendNPCsQuestEffect(Target, Target.GetQuestIndicator(questData.QuestPlayer));
			return res;
		}
	}
}
