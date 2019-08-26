using System;
using AmteScripts.Managers;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS
{
	public class AmtePlayer : GamePlayer
	{
		public DateTime LastDeath = DateTime.MinValue;
		public string LastKillerID;
		public BlacklistPlayer Blacklist;

		public DateTime LastActivity = DateTime.Now;
		public Point3D LastPosition = new Point3D(0, 0, 0);

		public AmtePlayer(GameClient client, DOLCharacters dbChar) : base(client, dbChar) {}

		public override int BountyPointsValue
		{
			get
			{
				if (RvrManager.Instance.IsInRvr(this))
					return (int)(1 + Level * 0.6);
				return 0;
			}
		}

		public override void Die(GameObject killer)
		{
			base.Die(killer);

			if (RvrManager.Instance.IsInRvr(this))
				return;

			LastDeath = DateTime.Now;
			// BlacklistMgr
			if (killer is AmtePlayer)
			{
				LastKillerID = killer.InternalID;
				BlacklistMgr.PlayerKilledByPlayer(this, (AmtePlayer)killer);
			}
			else
				LastKillerID = null;
		}

		public void SendMessage(string message, eChatType type = eChatType.CT_System, eChatLoc loc = eChatLoc.CL_SystemWindow)
		{
			Out.SendMessage(message, type, loc);
		}

		#region DB
		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);

			Blacklist = GameServer.Database.FindObjectByKey<BlacklistPlayer>(InternalID) ?? new BlacklistPlayer(this);
		}

		public override void SaveIntoDatabase()
		{
			base.SaveIntoDatabase();

			if (Blacklist.IsPersisted)
				GameServer.Database.SaveObject(Blacklist);
			else
				GameServer.Database.AddObject(Blacklist);
		}

		public override void DeleteFromDatabase()
		{
			base.DeleteFromDatabase();

			GameServer.Database.DeleteObject(Blacklist);
		}
		#endregion

		public override void CraftItem(ushort itemID)
		{
			if (JailMgr.IsPrisoner(this))
				return;

			LastActivity = DateTime.Now;
			base.CraftItem(itemID);
		}

		public override void UseSlot(int slot, int type)
		{
			if (JailMgr.IsPrisoner(this))
				return;
			base.UseSlot(slot, type);
		}
	}
}
