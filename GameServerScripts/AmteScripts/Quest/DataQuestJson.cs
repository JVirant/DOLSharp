using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DOL.GS.Quests
{
	/// <summary>
	/// This class hold the "code" about this quest (requirements, steps, actions, etc)
	/// </summary>
	public class DataQuestJson
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private DBDataQuestJson _db;

		public ushort Id { get; private set; } = 0;
		public string Name;
		public string Description;
		public string Summary;
		public string Story;
		public string Conclusion;
		public GameNPC Npc;

		public ushort MaxCount;
		public byte MinLevel;
		public byte MaxLevel;
		public int[] QuestDependencyIDs = new int[0];
		public eCharacterClass[] AllowedClasses = new eCharacterClass[0];

		public long RewardMoney;
		public long RewardXP;
		public int RewardCLXP;
		public int RewardRP;
		public int RewardBP;
		public int NbChooseOptionalItems;
		public List<ItemTemplate> OptionalRewardItemTemplates = new List<ItemTemplate>();
		public List<ItemTemplate> FinalRewardItemTemplates = new List<ItemTemplate>();

		/// <summary>
		/// GoalID to DataQuestJsonGoal
		/// </summary>
		public readonly Dictionary<int, DataQuestJsonGoal> Goals = new Dictionary<int, DataQuestJsonGoal>();

		public DataQuestJson()
		{
		}

		public void Notify(PlayerQuest questData, DOLEvent e, object sender, EventArgs args)
		{
			foreach (var goal in Goals.Values)
				if (goal.IsActive(questData))
					goal.NotifyActive(questData, e, sender, args);
			foreach (var goal in Goals.Values)
				goal.Notify(questData, e, sender, args);
		}

		public List<IQuestGoal> GetVisibleGoals(PlayerQuest data)
		{
			return data.GoalStates.Where(gs => gs.Active).Select(gs => Goals[gs.GoalId].ToQuestGoal(data, gs)).ToList();
		}

		public bool CheckQuestQualification(GamePlayer player)
		{
			if (MinLevel < player.Level && player.Level > MaxLevel)
				return false;

			lock (player.QuestList)
			{
				// the player is doing this quest
				if (player.QuestList.Where(q => q.Step != -1 && q is PlayerQuest).Cast<PlayerQuest>().Any(q => q.QuestId == Id))
					return false;
			}
			lock (player.QuestListFinished)
			{
				var count = player.QuestListFinished.Where(q => q is PlayerQuest json && json.Quest == this).Count();
				if (count >= MaxCount)
					return false;
			}

			return true;
		}

		public void FinishQuest(PlayerQuest data,  List<ItemTemplate> chosenItems)
		{
			int inventorySpaceRequired = FinalRewardItemTemplates.Count + chosenItems.Count;
			var player = data.QuestPlayer;
			if (!player.Inventory.IsSlotsFree(inventorySpaceRequired, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(string.Format("Your inventory is full, you need {0} free slot(s) to complete this quest.", inventorySpaceRequired), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			player.Out.SendSoundEffect(11, 0, 0, 0, 0, 0);
			player.GainExperience(GameLiving.eXPSource.Quest, RewardXP);
			player.AddMoney(RewardMoney);
			InventoryLogging.LogInventoryAction("(QUEST;" + Name + ")", player, eInventoryActionType.Quest, RewardMoney);
			if (RewardBP > 0)
				player.GainBountyPoints(RewardBP);
			if (RewardRP > 0)
				player.GainRealmPoints(RewardRP);

			foreach (var item in FinalRewardItemTemplates)
				GiveItem(player, item);
			foreach (var item in chosenItems)
				GiveItem(player, item);

			player.Out.SendNPCsQuestEffect(Npc, Npc.GetQuestIndicator(player));
			data.FinishQuest();
		}

		private static void GiveItem(GamePlayer player, ItemTemplate itemTemplate)
		{
			var item = GameInventoryItem.Create(itemTemplate);
			if (!player.ReceiveItem(null, item))
			{
				player.CreateItemOnTheGround(item);
				player.Out.SendMessage(string.Format("Your backpack is full, {0} is dropped on the ground.", itemTemplate.Name), eChatType.CT_Important, eChatLoc.CL_PopupWindow);
			}
		}

		public virtual void SaveIntoDatabase()
		{
			if (_db == null)
				_db = new DBDataQuestJson();
			_db.Name = Name;
			_db.Description = Description;
			_db.Summary = Summary;
			_db.Story = Story;
			_db.Conclusion = Conclusion;
			_db.MaxCount = MaxCount;
			_db.MinLevel = MinLevel;
			_db.MaxLevel = MaxLevel;
			_db.RewardMoney = RewardMoney;
			_db.RewardXP = RewardXP;
			_db.RewardCLXP = RewardCLXP;
			_db.RewardRP = RewardRP;
			_db.RewardBP = RewardBP;
			_db.NbChooseOptionalItems = NbChooseOptionalItems;
			_db.OptionalRewardItemTemplates = string.Join("|", OptionalRewardItemTemplates.Select(i => i.Id_nb));
			_db.FinalRewardItemTemplates = string.Join("|", FinalRewardItemTemplates.Select(i => i.Id_nb));
			_db.QuestDependency = string.Join("|", QuestDependencyIDs);
			_db.AllowedClasses = string.Join("|", AllowedClasses);
			_db.GoalsJson = JsonConvert.SerializeObject(Goals.Select(kv => new { Id = kv.Key, Type = kv.Value.GetType().FullName, Data = kv.Value.GetDatabaseJsonObject() }).ToArray());
			if (_db.IsPersisted)
				GameServer.Database.SaveObject(_db);
			else
			{
				GameServer.Database.AddObject(_db);
				Id = _db.Id;
			}
		}


		public DataQuestJson(DBDataQuestJson db)
		{
			Id = db.Id;
			Name = db.Name;
			Description = db.Description;
			Summary = db.Summary;
			Story = db.Story;
			Conclusion = db.Conclusion;
			Npc = WorldMgr.GetNPCsByNameFromRegion(db.NpcName, db.NpcRegion, eRealm.None).FirstOrDefault();
			if (Npc == null)
				throw new Exception($"Quest {db.Id}: can't find the npc {db.NpcName} in region {db.NpcRegion}");

			MaxCount = db.MaxCount;
			MinLevel = db.MinLevel;
			MaxLevel = db.MaxLevel;
			QuestDependencyIDs = (db.QuestDependency ?? "").Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => int.Parse(id)).ToArray();
			AllowedClasses = (db.AllowedClasses ?? "").Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => (eCharacterClass)int.Parse(id)).ToArray();

			RewardMoney = db.RewardMoney;
			RewardXP = db.RewardXP;
			RewardCLXP = db.RewardCLXP;
			RewardRP = db.RewardRP;
			RewardBP = db.RewardBP;
			NbChooseOptionalItems = db.NbChooseOptionalItems;

			var optionalTemplates = (db.OptionalRewardItemTemplates ?? "").Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
			var finalTemplates = (db.FinalRewardItemTemplates ?? "").Split('|').Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
			var items = GameServer.Database.FindObjectsByKey<ItemTemplate>(optionalTemplates.Union(finalTemplates));
			OptionalRewardItemTemplates = optionalTemplates.Select(id => items.FirstOrDefault(it => it.Id_nb == id)).ToList();
			FinalRewardItemTemplates = finalTemplates.Select(id => items.FirstOrDefault(it => it.Id_nb == id)).ToList();

			var goals = JsonConvert.DeserializeObject<JArray>(db.GoalsJson);
			foreach (var json in goals)
			{
				var (id, type, data) = (json.Value<ushort>("Id"), json.Value<string>("Type"), json.Value<dynamic>("Data"));
				DataQuestJsonGoal goal = null;
				foreach (Assembly script in ScriptMgr.GameServerScripts)
				{
					try
					{
						goal = (DataQuestJsonGoal)script.CreateInstance(type, false, BindingFlags.Default, null, new[] { this, id, data }, null, null);
						if (goal != null)
							break;
					}
					catch (Exception e)
					{
						log.Error(e);
					}
				}
				if (goal == null)
					throw new Exception($"Quest {db.Id}: can't load the goal id {id}, the goal is null");
				Goals.Add(id, goal);
			}
			if (!Goals.Values.Any(g => g is EndGoal))
			{
				var id = Goals.Keys.Max() + 1;
				Goals.Add(id, new EndGoal(
					this,
					id,
					new
					{
						Description = $"Talk to {Npc.Name} to get your reward",
						TargetName = db.NpcName,
						TargetRegion = db.NpcRegion,
						GiveItem = (string)null,
						StartGoalsDone = (List<int>)null,
						EndWhenGoalsDone = (List<int>)null,
					}));
			}

			Npc.QuestListToGive.Add(new PlayerQuest(null, this));
		}

		public void Unload()
		{
			var quest = Npc.QuestListToGive.OfType<PlayerQuest>().FirstOrDefault(q => q.QuestId == Id);
			if (quest != null)
				Npc.QuestListToGive.Remove(quest);
			foreach (var goal in Goals.Values)
				goal.Unload();
		}
	}
}
