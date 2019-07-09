using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class LowLevelHealer : GameNPC
    {
        public static Spell HealSpell;
        public static SpellLine HealSpellLine;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            SetOwnBrain(new BlankBrain());

            if(HealSpellLine == null)
                HealSpellLine = SkillBase.GetSpellLine("Regrowth Bard Spec");
            if (HealSpell == null)
                HealSpell = SkillBase.GetSpellByID(4951);

            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (IsCasting)
            {
                player.Out.SendMessage("Je suis occupé pour le moment.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            TurnTo(player);

            if (player.Level > 5)
                player.Out.SendMessage("Vous avez l'air de très bien vous débrouiller sans moi.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            else if (player.Health == player.MaxHealth)
                player.Out.SendMessage("Vous me semblez peu expérimenté, revenez me voir si vous avez besoin de soins !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            else
                player.Out.SendMessage("Vous me semblez peu expérimenté, peut etre puis-je vous [aider] ?", eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str) || !(source is GamePlayer)) return false;
            GamePlayer player = source as GamePlayer;

            if (IsCasting)
            {
                player.Out.SendMessage("Je suis occupé pour le moment.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            TurnTo(player);

            if (player.Level > 5)
                player.Out.SendMessage("Vous avez l'air de très bien vous débrouiller sans moi.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            else if (player.Health == player.MaxHealth)
                player.Out.SendMessage("Vous me semblez peu expérimenté, revenez me voir si vous avez besoin de soins !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            else if (str == "aider")
            {
                if (player.InCombat)
                    player.Out.SendMessage("Je ne peux pas vous aider si vous êtes en combat.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                else
                {
                    TargetObject = player;
                    CastSpell(HealSpell, HealSpellLine);
                }
            }
            return true;
        }
    }
}
