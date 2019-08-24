namespace DOL.GS.Scripts
{
    public class BindStone : GameInventoryItem
    {
        public override bool Use(GamePlayer player)
        {
            if (!JailMgr.IsPrisoner(player))
                player.MoveToBind();
            return true;
        }
    }
}
