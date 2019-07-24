/*
- Interaction
- R�ponse:
	- Texte
	- Spell animation
	- Emotes
- phrase/emotes al�atoire en cc g�n�ral
- Conditions:
	- Level
	- guilde
	- race
	- classe
	- prp
*/
using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS.Scripts
{
    public class TextNPCMerchant : GameMerchant, ITextNPC
	{
        public TextNPCPolicy TextNPCData { get; set; }

        public TextNPCMerchant()
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
			return TextNPCData.Condition.CanGiveQuest && TextNPCData.Condition.CheckAccess(player)
					? eQuestIndicator.Available
					: eQuestIndicator.None;
		}
        #endregion
	}
}