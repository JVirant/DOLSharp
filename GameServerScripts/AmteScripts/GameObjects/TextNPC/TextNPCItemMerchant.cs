using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Quests;
using DOL.GS.Scripts;

public class TextNPCItemMerchant : GameItemCurrencyMerchant, ITextNPC, IAmteNPC
{
	public override string MoneyKey
	{
		get => _moneyKey;
	}
	public TextNPCPolicy TextNPCData { get; set; }

	private string _moneyKey;
	private AmteCustomParam _moneyItemParam => new AmteCustomParam("money_item",
		() => MoneyKey,
		v => _moneyKey = v,
		null
	);

	public TextNPCItemMerchant()
	{
		TextNPCData = new TextNPCPolicy(this);
		SetOwnBrain(new TextNPCBrain());
	}

	public void SayRandomPhrase()
	{
		TextNPCData.SayRandomPhrase();
	}

	public override bool Interact(GamePlayer player)
	{
		if (!TextNPCData.CheckAccess(player) || !base.Interact(player))
			return false;
		return TextNPCData.Interact(player);
	}

	public override bool WhisperReceive(GameLiving source, string str)
	{
		if (!base.WhisperReceive(source, str))
			return false;
		return TextNPCData.WhisperReceive(source, str);
	}

	public override bool ReceiveItem(GameLiving source, InventoryItem item)
	{
		return TextNPCData.ReceiveItem(source, item);
	}

	public override void LoadFromDatabase(DataObject obj)
	{
		base.LoadFromDatabase(obj);
		TextNPCData.LoadFromDatabase(obj);
		DBBrainsParam[] data;
		if (!DBBrainsParam.MobXDBBrains.TryGetValue(obj.ObjectId, out data))
			data = new DBBrainsParam[0];
		_moneyItemParam.Value = data.FirstOrDefault(d => d.Param == _moneyItemParam.name)?.Value;

		if (!string.IsNullOrEmpty(MoneyKey))
		{
			m_itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(MoneyKey);
			if (m_itemTemplate != null)
				m_moneyItem = WorldInventoryItem.CreateFromTemplate(m_itemTemplate);
		}
	}

	public override void SaveIntoDatabase()
	{
		base.SaveIntoDatabase();
		TextNPCData.SaveIntoDatabase();
		DBBrainsParam[] data;
		if (!DBBrainsParam.MobXDBBrains.TryGetValue(InternalID, out data))
			data = new DBBrainsParam[0];
		var param = data.FirstOrDefault(d => d.Param == _moneyItemParam.name);
		if (param != null && param.Value != _moneyItemParam.Value)
		{
			param.Value = _moneyItemParam.Value;
			GameServer.Database.SaveObject(param);
		}
		else if (_moneyItemParam.Value != _moneyItemParam.defaultValue)
		{
			param = new DBBrainsParam
			{
				MobID = InternalID,
				Param = _moneyItemParam.name,
				Value = _moneyItemParam.Value,
			};
			GameServer.Database.AddObject(param);
			if (data.Length == 0)
				DBBrainsParam.MobXDBBrains.Add(InternalID, new []{param});
			else
			{
				data = data.Concat(new[] {param}).ToArray();
				DBBrainsParam.MobXDBBrains[InternalID] = data;
			}
		}
	}

	public override void DeleteFromDatabase()
	{
		base.DeleteFromDatabase();
		TextNPCData.DeleteFromDatabase();
		DBBrainsParam[] data;
		if (!DBBrainsParam.MobXDBBrains.TryGetValue(InternalID, out data))
			data = new DBBrainsParam[0];
		var param = data.FirstOrDefault(d => d.Param == _moneyItemParam.name);
		if (param != null)
			GameServer.Database.DeleteObject(param);
		DBBrainsParam.MobXDBBrains.Remove(InternalID);
	}

	public override eQuestIndicator GetQuestIndicator(GamePlayer player)
	{
		var result = base.GetQuestIndicator(player);
		if (result != eQuestIndicator.None)
			return result;

		foreach (var q in QuestListToGive.OfType<PlayerQuest>())
		{
			var quest = player.QuestList.OfType<PlayerQuest>().FirstOrDefault(pq => pq.QuestId == q.QuestId);
			if (quest == null)
				continue;
			if (quest.VisibleGoals.OfType<DataQuestJsonGoal.GenericDataQuestGoal>()
				.Any(g => g.Goal is EndGoal end && end.Target == this))
				return eQuestIndicator.Finish;
		}

		return TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None && TextNPCData.Condition.CheckAccess(player)
			? TextNPCData.Condition.CanGiveQuest
			: eQuestIndicator.None;
	}

	public AmteCustomParam GetCustomParam()
	{
		return _moneyItemParam;
	}

	public IList<string> DelveInfo()
	{
		return new List<string>
		{
			$"money_item: {m_moneyItem?.Item?.Name} (id_nb: {_moneyKey})",
		};
	}
}