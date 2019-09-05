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
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;
using DOL.Language;

namespace DOL.GS.Commands
{
	[Cmd("&Reload",
		ePrivLevel.Admin,
		"Commands.Admin.Reload.Description",
		"Commands.Admin.Reload.Usage.Mob",
        "Commands.Admin.Reload.Usage.Object",
        "Commands.Admin.Reload.Usage.Specs",
        "Commands.Admin.Reload.Usage.Spells"
		)]
	public class ReloadCommandHandler : ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static void SendSystemMessageBase(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Description"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageMob(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Mob"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Mob.Model"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Mob.Name"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Mob.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageObject(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Object"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Object.Model"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Object.Name"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Object.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			}
		}
		private static void SendSystemMessageRealm(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.ObjectMob.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Realm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			}
		}
		private static void SendSystemMessageName(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Name"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageModel(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Model"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}

		public void OnCommand(GameClient client, string[] args)
		{
			ushort region = 0;
			if (client.Player != null)
				region = client.Player.CurrentRegionID;
			string arg = "";
			int argLength = args.Length - 1;

			if (argLength < 1)
			{
				if (client.Player != null)
				{
					SendSystemMessageBase(client);
					SendSystemMessageMob(client);
					SendSystemMessageObject(client);
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Specs"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Usage.Spells"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				log.Info("'/reload command' Failed, review parameters.");
				return;
			}
			else if (argLength > 1)
			{
				if (args[2].ToLower() == "realm")
				{
					if (argLength == 2)
					{
						SendSystemMessageRealm(client);
						return;
					}

					if (args[3] == "0" || args[3].ToLower() == "none" || args[3] == "no" || args[3] == "n")
						arg = "None";
					else if (args[3] == "1" || args[3].ToLower() == "a" || args[3].ToLower() == "alb" || args[3].ToLower() == "albion")
						arg = "Albion";
					else if (args[3] == "2" || args[3].ToLower() == "m" || args[3].ToLower() == "mid" || args[3].ToLower() == "midgard")
						arg = "Midgard";
					else if (args[3] == "3" || args[3].ToLower() == "h" || args[3].ToLower() == "hib" || args[3].ToLower() == "hibernia")
						arg = "Hibernia";
					else
					{
						SendSystemMessageRealm(client);
						return;
					}
				}
				else if (args[2].ToLower() == "name")
				{
					if (argLength == 2)
					{
						SendSystemMessageName(client);
						return;
					}
					arg = String.Join(" ", args, 3, args.Length - 3);
				}
				else if (args[2].ToLower() == "model")
				{
					if (argLength == 2)
					{
						SendSystemMessageModel(client);
						return;
					}
					arg = args[3];
				}
			}

			if (args[1].ToLower() == "mob")
			{

				if (argLength == 1)
				{
					arg = "all";
					ReloadMobs(client.Player, region, arg, arg);
				}

				if (argLength > 1)
				{
					ReloadMobs(client.Player, region, args[2], arg);
				}
			}

			if (args[1].ToLower() == "object")
			{
				if (argLength == 1)
				{
					arg = "all";
					ReloadStaticItem(region, arg, arg);
				}

				if (argLength > 1)
				{
					ReloadStaticItem(region, args[2], arg);
				}
			}
			
			if (args[1].ToLower() == "spells")
			{
				SkillBase.ReloadDBSpells();
				int loaded = SkillBase.ReloadSpellLines();
				if (client != null) ChatUtil.SendSystemMessage(client, "Commands.Admin.Reload.Reloaded.Spells", loaded);
				log.Info(string.Format("Reloaded db spells and {0} spells for all lines !", loaded));
				return;
			}

			if (args[1].ToLower() == "specs")
			{
				int count = SkillBase.LoadSpecializations();
				if (client != null) client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Reload.Reloaded.Specs", count), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Info(string.Format("{0} specializations loaded.", count));
				return;
			}

			return;
		}

		private void ReloadMobs(GamePlayer player, ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("Region reload not supported from console.");
				return;
			}

			ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Admin.Reload.ReloadingMobs", arg1, arg2, region));

			int count = 0;

			foreach (GameNPC mob in WorldMgr.GetNPCsFromRegion(region))
			{
				if (!mob.LoadedFromScript)
				{
					if (arg1 == "all")
					{
						mob.RemoveFromWorld();

						Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
						if (mobs != null)
						{
							mob.LoadFromDatabase(mobs);
							mob.AddToWorld();
							count++;
						}
					}

					if (arg1 == "realm")
					{
						eRealm realm = eRealm.None;
						if (arg2 == "None") realm = eRealm.None;
						if (arg2 == "Albion") realm = eRealm.Albion;
						if (arg2 == "Midgard") realm = eRealm.Midgard;
						if (arg2 == "Hibernia") realm = eRealm.Hibernia;

						if (mob.Realm == realm)
						{
							mob.RemoveFromWorld();

							Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}

					if (arg1 == "name")
					{
						if (mob.Name == arg2)
						{
							mob.RemoveFromWorld();

							Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}

					if (arg1 == "model")
					{
						if (mob.Model == Convert.ToUInt16(arg2))
						{
							mob.RemoveFromWorld();

							WorldObject mobs = GameServer.Database.FindObjectByKey<WorldObject>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}
				}
			}

			ChatUtil.SendSystemMessage(player, "Commands.Admin.Reload.Reloaded.Spells", count);
		}

		private void ReloadStaticItem(ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("Region reload not supported from console.");
				return;
			}

			foreach (GameStaticItem staticItem in WorldMgr.GetStaticItemFromRegion(region))
			{
				if (!staticItem.LoadedFromScript)
				{
					if (arg1 == "all")
					{
						staticItem.RemoveFromWorld();

						WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
						if (obj != null)
						{
							staticItem.LoadFromDatabase(obj);
							staticItem.AddToWorld();
						}
					}

					if (arg1 == "realm")
					{
						eRealm realm = eRealm.None;
						if (arg2 == "None") realm = eRealm.None;
						if (arg2 == "Albion") realm = eRealm.Albion;
						if (arg2 == "Midgard") realm = eRealm.Midgard;
						if (arg2 == "Hibernia") realm = eRealm.Hibernia;

						if (staticItem.Realm == realm)
						{
							staticItem.RemoveFromWorld();

							WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}

					if (arg1 == "name")
					{
						if (staticItem.Name == arg2)
						{
							staticItem.RemoveFromWorld();

							WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}

					if (arg1 == "model")
					{
						if (staticItem.Model == Convert.ToUInt16(arg2))
						{
							staticItem.RemoveFromWorld();

							WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}
				}
			}
		}
	}
}
