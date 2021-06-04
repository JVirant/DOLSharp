using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Linq;

namespace DOL.GS.Quests
{
	public class EndGoal : DataQuestJsonGoal
	{
		private readonly string m_description;
		private GameNPC m_target;

		public override string Description => m_description;
		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;

		public EndGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId)
		{
			m_description = db.Description;
			m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ??  "", (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
			if (m_target == null)
				m_target = quest.Npc;
		}

		public override object GetDatabaseJsonObject()
		{
			return new
			{
				Description = m_description,
				TargetName = m_target.Name,
				TargetRegion = m_target.CurrentRegionID,
			};
		}

		public override void Notify(PlayerQuest questData, PlayerGoalState goalData, DOLEvent e, object sender, EventArgs args)
		{
			var player = questData.QuestPlayer;
			// Enemy of player with quest was killed, check quests and steps
			if (e == GameObjectEvent.InteractWith && args is InteractWithEventArgs interact && interact.Target.Name == m_target.Name && interact.Target.CurrentRegion == m_target.CurrentRegion)
				player.Out.SendQuestRewardWindow(interact.Target as GameNPC, player, questData);
			if (e == GamePlayerEvent.QuestRewardChosen && args is QuestRewardChosenEventArgs rewardArgs && rewardArgs.QuestID == questData.QuestId)
			{
				if (questData.Quest.NbChooseOptionalItems != rewardArgs.CountChosen && questData.Quest.OptionalRewardItemTemplates.Count >= questData.Quest.NbChooseOptionalItems)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "RewardQuest.Notify"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
					return;
				}

				var items = questData.Quest.OptionalRewardItemTemplates.Where((item, idx) => rewardArgs.ItemsChosen.Contains(idx)).ToList();
				questData.Quest.FinishQuest(questData, items);
			}
		}
	}
}
