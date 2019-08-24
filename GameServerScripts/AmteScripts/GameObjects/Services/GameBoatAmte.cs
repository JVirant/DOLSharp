using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Movement;

namespace DOL.GS
{
    public class GameBoatAmte : GameMovingObject
    {
        public override int MAX_PASSENGERS
        {
            get { return Size; }
        }

        public override int SLOT_OFFSET
        {
            get { return 2; }
        }

        public override ushort Type()
        {
            return 2;
        }

        public GameBoatAmte()
        {
            Size = 16;
            Realm = eRealm.Door;
        	Flags = eFlags.PEACE;
            Model = 1613;
            MaxSpeedBase = 600;
            Level = 0;
            Name = "Bateau";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!IsWithinRadius(player, 1024))
            {
                player.Out.SendMessage("Vous êtes trop loin de " + Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (GetFreeArrayLocation() == -1)
                player.Out.SendMessage("Il n'y a plus de place sur " + Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else
                player.MountSteed(this, false);

            return true;
        }

        #region Path
        private string m_PathName = "";
        public string PathName
        {
            get { return m_PathName; }
            set
            {
                m_PathName = value;
                Reset();
            }
        }

        public override short MaxSpeedBase
        {
            get
            {
                return base.MaxSpeedBase;
            }
            set
            {
                base.MaxSpeedBase = value;
                Reset();
            }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            SetOwnBrain(new BlankBrain());
            Reset();
            return true;
        }

        public void Reset()
        {
            if(IsMoving)
                StopMoving();
            if (m_PathName != "")
            {
                CurrentWayPoint = MovementMgr.LoadPath(m_PathName);
                MoveOnPath(MaxSpeedBase);
            }
        }
        #endregion

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

            PathName = npc.EquipmentTemplateID;
        }

        public override void SaveIntoDatabase()
        {
            Mob mob;
            if (InternalID != null)
                mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
            else
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

            mob.EquipmentTemplateID = PathName;

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

