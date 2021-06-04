/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Holds all the DataQuests available
	/// </summary>
	[DataTable(TableName = "DataQuestJson")]
	public class DBDataQuestJson : DataObject
	{
		private ushort m_id;
		private string m_name;
		private string m_description;
		private string m_summary;
		private string m_story;
		private string m_conclusion;

		private string m_npcName;
		private ushort m_npcRegion;

		private ushort m_maxCount;
		private byte m_minLevel;
		private byte m_maxLevel;
		private string m_questDependency;
		private string m_allowedClasses;

		private long m_rewardMoney;
		private long m_rewardXP;
		private int m_rewardCLXP;
		private int m_rewardRP;
		private int m_rewardBP;
		private int m_nbChooseOptionalItems;
		private string m_optionalRewardItemTemplates;
		private string m_finalRewardItemTemplates;

		private string m_goalsJson;

		public DBDataQuestJson()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public ushort Id
		{
			get { return m_id; }
			set { m_id = value; }
		}

		/// <summary>
		/// The name of this quest
		/// </summary>
		[DataElement(Varchar = 255, AllowDbNull = false)]
		public string Name
		{
			get { return m_name; }
			set { m_name = value; Dirty = true; }
		}

		/// <summary>
		/// Description to show to start quest
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Description
		{
			get { return m_description; }
			set { m_description = value; Dirty = true; }
		}

		[DataElement(AllowDbNull = true)]
		public string Summary
		{
			get { return m_summary; }
			set { m_summary = value; Dirty = true; }
		}
		[DataElement(AllowDbNull = true)]
		public string Story
		{
			get { return m_story; }
			set { m_story = value; Dirty = true; }
		}
		[DataElement(AllowDbNull = true)]
		public string Conclusion
		{
			get { return m_conclusion; }
			set { m_conclusion = value; Dirty = true; }
		}

		[DataElement(AllowDbNull = false)]
		public string NpcName
		{
			get { return m_npcName; }
			set { m_npcName = value; Dirty = true; }
		}
		[DataElement(AllowDbNull = false)]
		public ushort NpcRegion
		{
			get { return m_npcRegion; }
			set { m_npcRegion = value; Dirty = true; }
		}


		/// <summary>
		/// Max number of times a player can do this quest
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public ushort MaxCount
		{
			get { return m_maxCount; }
			set { m_maxCount = value; Dirty = true; }
		}

		/// <summary>
		/// Minimum level a player has to be to start this quest
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte MinLevel
		{
			get { return m_minLevel; }
			set { m_minLevel = value; Dirty = true; }
		}

		/// <summary>
		/// Max level a player can be and still do this quest
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte MaxLevel
		{
			get { return m_maxLevel; }
			set { m_maxLevel = value; Dirty = true; }
		}

		/// <summary>
		/// Reward Money to give at each step, 0 for none
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public long RewardMoney
		{
			get { return m_rewardMoney; }
			set { m_rewardMoney = value; Dirty = true; }
		}

		/// <summary>
		/// Reward XP to give at each step, 0 for none
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public long RewardXP
		{
			get { return m_rewardXP; }
			set { m_rewardXP = value; Dirty = true; }
		}
		
		/// <summary>
		/// Reward CLXP to give at each step, 0 for none
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int RewardCLXP
		{
			get { return m_rewardCLXP; }
			set { m_rewardCLXP = value; Dirty = true; }
		}
		
		/// <summary>
		/// Reward RP to give at each step, 0 for none
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int RewardRP
		{
			get { return m_rewardRP; }
			set { m_rewardRP = value; Dirty = true; }
		}
		
		/// <summary>
		/// Reward BP to give at each step, 0 for none
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int RewardBP
		{
			get { return m_rewardBP; }
			set { m_rewardBP = value; Dirty = true; }
		}

		[DataElement(AllowDbNull = false)]
		public int NbChooseOptionalItems
		{
			get { return m_nbChooseOptionalItems; }
			set { m_nbChooseOptionalItems = value; Dirty = true; }
		}

		/// <summary>
		/// The ItemTemplate id_nb(s) to give as a optional rewards
		/// Format:  id_nb1|id_nb2 with first character being the number of choices
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string OptionalRewardItemTemplates
		{
			get { return m_optionalRewardItemTemplates; }
			set { m_optionalRewardItemTemplates = value; Dirty = true; }
		}

		/// <summary>
		/// The ItemTemplate id_nb(s) to give as a final reward
		/// Format:  id_nb1|id_nb2
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string FinalRewardItemTemplates
		{
			get { return m_finalRewardItemTemplates; }
			set { m_finalRewardItemTemplates = value; Dirty = true; }
		}

		/// <summary>
		/// The id of other quests that need to be done before this quest can be offered.
		/// 12|14... Can be null if no dependency
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string QuestDependency
		{
			get { return m_questDependency; }
			set { m_questDependency = value; Dirty = true; }
		}

		/// <summary>
		/// Player classes that can do this quest.  Null for all.
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string AllowedClasses
		{
			get { return m_allowedClasses; }
			set { m_allowedClasses = value; Dirty = true; }
		}

		/// <summary>
		/// The step data serialized as a json object where the key is the goal id
		/// Each item of the array must have a type property which load the correct code for this step
		/// Format: [{"id":1,"type":"DOL.GS.Quests.CollectGoal","data":{"collectItemTemplate":"quest1","to":["Guard",1]}},...]
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string GoalsJson
		{
			get { return m_goalsJson; }
			set { m_goalsJson = value; Dirty = true; }
		}
	}
}
