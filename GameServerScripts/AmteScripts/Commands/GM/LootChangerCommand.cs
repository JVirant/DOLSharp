using System;
using System.Collections;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
		"&lootchanger",
		ePrivLevel.GM,
		"Gestion des loots changer",
        "'/lootchanger add <receive item> <give item> '",
        "'/lootchanger remove <receive item/all>'",
        "'/lootchanger info'")]
    public class LootChangerCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (LootChangerGenerator.AddOrChangeLoot(client.Player.TargetObject as GameNPC, client, args) == -1)
                DisplaySyntax(client);
        }
    }
}
