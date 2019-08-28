using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Scripts;

public class AmteMob : GameNPC, IAmteNPC
{
	private readonly Dictionary<string, DBBrainsParam> _nameXcp = new Dictionary<string, DBBrainsParam>();

	private readonly AmteCustomParam _linkParam;

	public AmteMob()
	{
		SetOwnBrain(new AmteMobBrain(Brain));
		_linkParam = new AmteCustomParam(
			"link",
			() => ((AmteMobBrain) Brain).AggroLink.ToString(),
			v => ((AmteMobBrain) Brain).AggroLink = int.Parse(v),
			"0");
	}

	public AmteMob(INpcTemplate npc)
		: base(npc)
	{
		SetOwnBrain(new AmteMobBrain(Brain));
		_linkParam = new AmteCustomParam(
			"link",
			() => ((AmteMobBrain) Brain).AggroLink.ToString(),
			v => ((AmteMobBrain) Brain).AggroLink = int.Parse(v),
			"0");
	}

	public override bool IsFriend(GameNPC npc)
	{
		if (npc.Brain is IControlledBrain)
			return GameServer.ServerRules.IsSameRealm(this, npc, true);
		if (Faction == null && npc.Faction == null)
			return npc.Name == Name || (!string.IsNullOrEmpty(npc.GuildName)  && npc.GuildName == GuildName);
		return base.IsFriend(npc);
	}

	public override void LoadFromDatabase(DataObject obj)
	{
		base.LoadFromDatabase(obj);

		var data = GameServer.Database.SelectObjects<DBBrainsParam>("`MobID` = '" + obj.ObjectId + "'");
		for (var cp = GetCustomParam(); cp != null; cp = cp.next)
		{
			var cp1 = cp;
			var param = data.Where(o => o.Param == cp1.name).FirstOrDefault();
			if (param == null)
				continue;
			if (_nameXcp.ContainsKey(cp.name))
			{
				GameServer.Database.DeleteObject(param);
				continue;
			}
			try
			{
				cp.Value = param.Value;
			}
			catch (Exception e)
			{
				
			}
			_nameXcp.Add(cp.name, param);
		}

		// load some stats from the npctemplate
		if (NPCTemplate != null && !NPCTemplate.ReplaceMobValues)
		{
			if (NPCTemplate.Spells != null) this.Spells = NPCTemplate.Spells;
			if (NPCTemplate.Styles != null) this.Styles = NPCTemplate.Styles;
			if (NPCTemplate.Abilities != null)
			{
				lock (m_lockAbilities)
				{
					foreach (Ability ab in NPCTemplate.Abilities)
						m_abilities[ab.KeyName] = ab;
				}
			}
		}
	}

	public override void SaveIntoDatabase()
	{
		base.SaveIntoDatabase();

		DBBrainsParam param;
		for (var cp = GetCustomParam(); cp != null; cp = cp.next)
			if (_nameXcp.TryGetValue(cp.name, out param))
			{
				param.Value = cp.Value;
				GameServer.Database.SaveObject(param);
				
			}
			else if (cp.defaultValue != cp.Value)
			{
				param = new DBBrainsParam
				        {
				        	MobID = InternalID,
				        	Param = cp.name,
				        	Value = cp.Value
				        };
				_nameXcp.Add(cp.name, param);
				GameServer.Database.AddObject(param);
			}
	}

	public override void DeleteFromDatabase()
	{
		base.DeleteFromDatabase();
		_nameXcp.Values.ForEach(o => GameServer.Database.DeleteObject(o));
	}

	public virtual AmteCustomParam GetCustomParam()
	{
		return _linkParam;
	}

	public virtual IList<string> DelveInfo()
	{
		var list = new List<string>();
		for (var cp = GetCustomParam(); cp != null; cp = cp.next)
			list.Add(" - " + cp.name + ": " + cp.Value);
		return list;
	}
}
