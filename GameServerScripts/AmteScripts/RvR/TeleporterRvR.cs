using AmteScripts.Managers;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class TeleporterRvR : GameNPC
    {
        private bool _isBusy;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            SetOwnBrain(new BlankBrain());
            return true;
        }

        private bool _BaseSay(GamePlayer player, string str = "Partir")
        {
            if (_isBusy)
            {
                player.Out.SendMessage("Je suis occupé pour le moment !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            TurnTo(player);

            if (!RvrManager.Instance.IsInRvr(player) &&
                (!RvrManager.Instance.IsOpen || player.Level < 20 || (str != "Pret" && str != "Prêt" && str != "Partir")))
            {
                player.Out.SendMessage(
                    "Bonjour " + player.Name + ", je ne peux rien faire pour vous pour le moment !\r\nRevenez plus tard !",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            return false;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (!RvrManager.Instance.IsOpen && RvrManager.Instance.IsRvRRegion(player.CurrentRegionID))
            {
                _Teleport(player);
                return true;
            }

            if (_BaseSay(player)) return true;

            if (RvrManager.Instance.IsInRvr(player))
                player.Out.SendMessage(
                    "Pff, tu es trop une poule mouillée pour rester ?!\r\n[Partir]",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
            else
                player.Out.SendMessage(
                        "Bonjour " + player.Name + ", je peux vous envoyer au combat ! [Prêt] ?!",
                        eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str) || !(source is GamePlayer)) return false;
            GamePlayer player = source as GamePlayer;

            if (_BaseSay(player, str)) return true;

            _Teleport(player);
            return true;
        }

        private void _Teleport(GamePlayer player)
        {
            _isBusy = true;
            RegionTimer TimerTL = new RegionTimer(this, _Teleportation);
            TimerTL.Properties.setProperty("player", player);
            TimerTL.Start(3000);
            foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                players.Out.SendSpellCastAnimation(this, 1, 20);
                players.Out.SendEmoteAnimation(player, eEmote.Bind);
            }
        }

        private int _Teleportation(RegionTimer timer)
        {
            _isBusy = false;
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
            if (player == null) return 0;
            if (player.InCombat)
                player.Out.SendMessage("Vous ne pouvez pas être téléporté en étant en combat !",
                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            else
            {
                if (!RvrManager.Instance.IsOpen || RvrManager.Instance.IsInRvr(player))
                    RvrManager.Instance.RemovePlayer(player);
                else
                    RvrManager.Instance.AddPlayer(player);
            }
            return 0;
        }
    }
}