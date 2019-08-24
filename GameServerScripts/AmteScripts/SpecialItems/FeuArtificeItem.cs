/************************************************************************************
 *	Utilisation:                                                                    *
 *	- Id_nb:                                                                        *
 *		Doit commencer par "feuarti" (/item savetemplate feuartiXXXX)               *
 *	- Durability:                                                                   *
 *		Règle le temps du feu en ticks (1 tick = 2.5s)                              *
 ************************************************************************************/

using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.Scripts
{
	public class FeuArtificeEvent
	{
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseSlot));
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseSlot));
		}

		protected static void PlayerUseSlot(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;
			if (player == null) return;

			UseSlotEventArgs uArgs = (UseSlotEventArgs)args;

			if (player.Inventory.GetItem((eInventorySlot)uArgs.Slot) != null)
			{
                //On vérifie que l'item
                InventoryItem item = player.Inventory.GetItem((eInventorySlot)uArgs.Slot);
                if (!item.Id_nb.StartsWith("feuarti"))
                    return;

			    if (!player.Inventory.RemoveCountFromStack(item, 1))
			        return;
                InventoryLogging.LogInventoryAction(player, "(feu artifice)", eInventoryActionType.Other, item.Template);

                Dictionary<int, GameNPC> mobs = CreateMobs(player);
			    RegionTimer timer = new RegionTimer(mobs[0], FeuCallback);
			    timer.Properties.setProperty("mobs", mobs);
			    timer.Properties.setProperty("time", 0);
			    timer.Properties.setProperty("timemax", (int)(item.Durability/2.5));

                int time = Math.Max(item.Condition, 10);
			    timer.Start(time*1000);
			    
			    player.Out.SendMessage("Le feu d'artifice va commencer dans "+time+" secondes.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

	    private static int FeuCallback(RegionTimer callingTimer)
	    {
            Dictionary<int, GameNPC> mobs = callingTimer.Properties.getProperty<Dictionary<int, GameNPC>>("mobs", null);
            if (mobs == null)
                return 0;

            int time = callingTimer.Properties.getProperty("time", 0);
            int timemax = callingTimer.Properties.getProperty("timemax", 0);
            time++;
	        callingTimer.Properties.setProperty("time", time);

	    	if(time > (timemax+1))
	    	{
				foreach (KeyValuePair<int, GameNPC> mob in mobs)
				{
					mob.Value.Delete();
					mob.Value.DeleteFromDatabase();
				}
				return 0;
	    	}
            if (time > timemax)
                return 1500;

            GameNPC npc1, npc2, npc3, npc4, npc5;
            int effect1, effect2;
	        switch(Util.Random(1,3))
	        {
	            case 1:
                    npc1 = mobs[3];
                    npc2 = mobs[5];
                    npc3 = mobs[7];
                    npc4 = mobs[9];

                    if (Util.Random(0, 1) == 1)
                        npc5 = mobs[15];
                    else
                        npc5 = mobs[14];
                    effect1 = Util.Random(5811, 5815);
                    effect2 = Util.Random(5811, 5815);
	                break;
	                
                case 2:
                    npc1 = mobs[10];
                    npc2 = mobs[11];
                    npc3 = mobs[12];
                    npc4 = mobs[13];
                    npc5 = mobs[15];
                    effect1 = Util.Random(5811, 5815);
                    effect2 = Util.Random(5811, 5815);
	                break;
	                
                default:
                    npc1 = mobs[2];
                    npc2 = mobs[4];
                    npc3 = mobs[6];
                    npc4 = mobs[8];
                    if (Util.Random(0, 1) == 1)
                        npc5 = mobs[15];
                    else
                        npc5 = mobs[14];
	                
                    effect1 = Util.Random(5811, 5815);
                    effect2 = Util.Random(5811, 5815);
	                break;
	        }

            foreach (GamePlayer player in mobs[0].GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(mobs[0], mobs[0], 71, 0, false, 0x01);
                player.Out.SendSpellEffectAnimation(mobs[0], npc1, (ushort)effect1, 0, false, 0x01);
                player.Out.SendSpellEffectAnimation(mobs[0], npc2, (ushort)effect1, 0, false, 0x01);
                player.Out.SendSpellEffectAnimation(mobs[0], npc3, (ushort)effect1, 0, false, 0x01);
                player.Out.SendSpellEffectAnimation(mobs[0], npc4, (ushort)effect1, 0, false, 0x01);
                player.Out.SendSpellEffectAnimation(mobs[0], npc5, (ushort)effect2, 0, false, 0x01);

                //son
                player.Out.SendSpellEffectAnimation(player, player, 8300, 0, false, 0x01);
            }
	        
            return 2500;
	    }

        private static Dictionary<int, GameNPC> CreateMobs(GameObject obj)
	    {
            Dictionary<int, GameNPC> mobs = new Dictionary<int, GameNPC>();
	        for(int i = 0; i < 16; i++)
	        {
	            GameNPC npc = new GameNPC
	                          	{
	                          		Name = "Feu" + i,
	                          		Flags =
	                          			GameNPC.eFlags.PEACE | GameNPC.eFlags.FLYING |
										GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.DONTSHOWNAME,
	                          		Level = 1,
	                          		Model = 1,
	                          		Realm = 0,
	                          		Z = obj.Z + 800,
	                          		Y = obj.Y,
	                          		X = obj.X,
	                          		CurrentRegion = obj.CurrentRegion
	                          	};

	        	npc.SetOwnBrain(new BlankBrain());
                mobs[i] = npc;
	        }

	        //Pied
            mobs[0].Z = obj.Z;
	        
	        //Pyramide:
	        //Base
            mobs[2].X += 250;
            mobs[2].Y += 250;
            mobs[3].X += 250;
            mobs[4].X += 250;
            mobs[4].Y -= 250;
            mobs[5].Y -= 250;
            mobs[6].X -= 250;
            mobs[6].Y -= 250;
            mobs[7].X -= 250;
            mobs[8].X -= 250;
            mobs[8].Y += 250;
            mobs[9].Y += 250;
	        
	        //2e étage
            mobs[10].Z += 250;
            mobs[10].X += 125;
            mobs[10].Y += 125;
            mobs[11].Z += 250;
            mobs[11].X += 125;
            mobs[11].Y -= 125;
            mobs[12].Z += 250;
            mobs[12].X -= 125;
            mobs[12].Y -= 125;
            mobs[13].Z += 250;
            mobs[13].X -= 125;
            mobs[13].Y += 125;
	        
            mobs[14].Z += 250;
            mobs[14].Size = 75;
	        
	        //Sommet
            mobs[15].Z += 500;
            mobs[15].Size = 100;

            foreach (KeyValuePair<int, GameNPC> mob in mobs)
                mob.Value.AddToWorld();
	        
	        return mobs;
	    }
    }
}
