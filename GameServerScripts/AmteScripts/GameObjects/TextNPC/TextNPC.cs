/**
 * Created by Virant "Dre" Jérémy for Amtenael
 */
using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS.Scripts
{
	public class TextNPC : AmteMob, ITextNPC
	{
        public TextNPCPolicy TextNPCData { get; set; }

        public TextNPC()
        {
            TextNPCData = new TextNPCPolicy(this);
            SetOwnBrain(new TextNPCBrain());
        }

        #region TextNPCPolicy
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
		}

		public override void SaveIntoDatabase()
		{
			base.SaveIntoDatabase();
			TextNPCData.SaveIntoDatabase();
		}

        public override void DeleteFromDatabase()
        {
            base.DeleteFromDatabase();
            TextNPCData.DeleteFromDatabase();
        }

		public override eQuestIndicator GetQuestIndicator(GamePlayer player)
		{
			var result = base.GetQuestIndicator(player);
			if (result != eQuestIndicator.None)
				return result;
			return TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None && TextNPCData.Condition.CheckAccess(player)
				? TextNPCData.Condition.CanGiveQuest
				: eQuestIndicator.None;
		}
        #endregion
	}

    /// <summary>
    /// Provided only for compatibility
    /// </summary>
    public class EchangeurNPC : TextNPC { }
}