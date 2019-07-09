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
using DOL.Database;

namespace DOL.GS
{
	public class LootGeneratorMoneyAmte : LootGeneratorBase
	{
        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);

            int lvl = (mob.Level < 0 ? 1 : mob.Level);
            int minLoot = (int)(2 + lvl*lvl*Math.Log(lvl + 1, 3)*2.6);

            long moneyCount = minLoot + Util.Random(minLoot >> 1);
            moneyCount = (long) (moneyCount*ServerProperties.Properties.MONEY_DROP);

            var money = new ItemTemplate
                            {
                                Model = 488,
                                Name = "bag of coins",
                                Level = 0,
                                Price = moneyCount
                            };

            loot.AddFixed(money, 1);
            return loot;
        }
	}
}
