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
using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.Behaviour;
using DOL.Language;
using System.Numerics;

namespace DOL.GS.Behaviour.Actions
{
    [ActionAttribute(ActionType = eActionType.Teleport,DefaultValueQ=0)]
    public class TeleportAction : AbstractAction<GameLocation,int>
    {               

        public TeleportAction(GameNPC defaultNPC,  Object p, Object q)
            : base(defaultNPC, eActionType.Teleport, p, q)
        {            
            }


        public TeleportAction(GameNPC defaultNPC,  GameLocation location, int fuzzyRadius)
            : this(defaultNPC,  (object)location, (object)fuzzyRadius) { }
        


        public override void Perform(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = BehaviourUtils.GuessGamePlayerFromNotify(e, sender, args);
            GameLocation location = P;
            int radius = Q;

            if (location.Name != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Behaviour.TeleportAction.TeleportedToLoc", player, location.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            location.Position += new Vector3(Util.Random(-radius, radius), Util.Random(-radius, radius), 0);
            player.MoveTo(location);
        }
    }
}
