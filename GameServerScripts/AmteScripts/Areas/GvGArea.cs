using System;
using System.Collections.Generic;
using AmteScripts.Managers;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace AmteScripts.Areas
{
	public class GvGArea : Area.Circle, IGvGArea
	{
		public bool Active { get; set; }
		public Guild Guild { get; set; }
		public IList<IGvGGuard> Gardes { get; set; }
		public GvGLord Lord { get; set; }
		public readonly DBGvGArea Db;

		public GvGArea(string name, int x, int y, ushort region, ushort radius) : base(name, x, y, 0, radius)
		{
			Db = new DBGvGArea
			     {
			     	Dirty = true,
			     	Type = GetType().ToString(),
			     	Name = name,
			     	X = x,
			     	Y = y,
			     	Region = region,
			     	Radius = radius,
			     };

		}

		public GvGArea(DBGvGArea db)
		{
			Db = db;
			Guild = GuildMgr.GetGuildByGuildID(db.GuildID);
		}

		public bool TakeControl(GamePlayer player, bool force)
		{
			if (!Active) //Zone désactivé
			{
				if (!force)
					player.Out.SendMessage("Cette zone est désactivé pour le moment.",
										   eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (player.Guild == null) //Joueur sans guilde
			{
				if (!force)
					player.Out.SendMessage("Vous ne pouvez pas prendre le contrôle d'une zone sans guilde !",
										   eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (!player.Guild.HasRank(player, Guild.eRank.Claim)) //Acces du joueur
			{
				if (!force)
					player.Out.SendMessage("Vous n'avez pas l'accès pour prendre cette zone !",
										   eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (Guild == player.Guild) //Guilde du joueur
			{
				if (!force)
					player.Out.SendMessage("Cette zone appartient déjà à votre guilde !",
										   eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			if (Gardes.Count > 0)
			{
				if (!force)
					player.Out.SendMessage("Vous devez tuer tous les gardes avant de pouvoir occuper la zone !",
										   eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			//TODO

			return true;
		}

		public override void LoadFromDatabase(DBArea area) { }

		public void SaveIntoDatabase()
		{
			
		}

		public void DeleteFromDatabase()
		{
			
		}
	}
}
