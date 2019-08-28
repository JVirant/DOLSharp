using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class PlaceAssise : GameBoat
	{
		public override int MAX_PASSENGERS
		{
			get { return 1; }
		}

		public override int SLOT_OFFSET
		{
			get { return 1; }
		}

		public override ushort Type()
		{
			return 2;
		}

		public PlaceAssise()
		{
			Realm = eRealm.None;
			Flags = eFlags.PEACE;
			Model = 1293;
			MaxSpeedBase = 0;
			Level = 0;
			Name = "Place assise";;
            SetOwnBrain(new BlankBrain());
		}

		public override bool Interact(GamePlayer player)
		{
			if (!IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
			{
				player.Out.SendMessage("Vous êtes trop loin pour vous asseoir.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (GetFreeArrayLocation() == -1)
				player.Out.SendMessage("La place est déjà prise.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			else
				player.MountSteed(this, false);

			return true;
		}

		#region Database
		public override void LoadFromDatabase(DataObject obj)
		{
			InternalID = obj.ObjectId;

			if (!(obj is Mob)) return;
			Mob npc = (Mob)obj;
			Name = npc.Name;
			GuildName = npc.Guild;
			X = npc.X;
			Y = npc.Y;
			Z = npc.Z;
			m_Heading = (ushort)(npc.Heading & 0xFFF);
			m_maxSpeedBase = (short)npc.Speed;	// TODO db has currntly senseless information here, mob type db required
			if (m_maxSpeedBase == 0)
				m_maxSpeedBase = 600;
			m_currentSpeed = 0;
			CurrentRegionID = npc.Region;
			Realm = (eRealm)npc.Realm;
			Model = npc.Model;
			Size = npc.Size;
			Level = npc.Level;
			Flags = eFlags.PEACE;
		}

		public override void SaveIntoDatabase()
		{
			Mob mob = null;
			if (InternalID != null)
				mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
			if (mob == null)
				mob = new Mob();

			mob.Name = Name;
			mob.Guild = GuildName;
			mob.X = X;
			mob.Y = Y;
			mob.Z = Z;
			mob.Heading = Heading;
			mob.Speed = MaxSpeedBase;
			mob.Region = CurrentRegionID;
			mob.Realm = (byte)Realm;
			mob.Model = Model;
			mob.Size = Size;
			mob.Level = Level;
			mob.ClassType = GetType().ToString();

			if (InternalID == null)
			{
				GameServer.Database.AddObject(mob);
				InternalID = mob.ObjectId;
			}
			else
				GameServer.Database.SaveObject(mob);
		}
		#endregion
	}
}
