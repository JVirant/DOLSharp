using System;
using System.Collections;
using System.Collections.Generic;
using AmteScripts.Managers;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class GuarksRing
	{
		public static Lieu[] Lieux = new[] {
			new Lieu("Dysfonctionnement de l'anneau", 333521, 394153, 16913, 0, 51, false), // Obligatoirement en 1er
			new Lieu("Araich", 496327, 524071, 3128, 858, 51, false),
			new Lieu("Eronig", 411902, 382014, 4977, 1028, 51, false),
			new Lieu("Aimital", 351909, 449663, 3194, 0, 51, false),
			new Lieu("Eskoth", 371046, 488096, 3176, 0, 51, false),
			new Lieu("Dogmak", 283289, 458794, 3515, 0, 51, false),
			new Lieu("Xogob", 350753, 391265, 3745, 0, 51, false),
			new Lieu("Angeruak", 59069, 28633, 11252, 1006, 92, true)
		};

		private const string RING_IS_AMULETTE = "GUARKS_RING_IS_AMULETTE";
		private const string RING_TARGET = "GUARKS_RING_TARGET";
		private const string RING_TIMER = "GUARKS_RING_TIMER";
		private static List<string> _playerIDs = new List<string>();

		[GameServerStartedEvent]
		public static void Init(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(PlayerInventoryEvent.ItemEquipped, new DOLEventHandler(ItemEquipped));
			GameEventMgr.AddHandler(PlayerInventoryEvent.ItemUnequipped, new DOLEventHandler(ItemUnequipped));
			GameEventMgr.AddHandler(GameLivingEvent.Say, new DOLEventHandler(Say));
		}

		public static void Say(DOLEvent e, object sender, EventArgs args)
		{
			if (!(sender is GamePlayer player))
				return;
			if (!_playerIDs.Contains(player.InternalID))
				return;
			SayEventArgs arg = (SayEventArgs)args;

			bool amulette = player.TempProperties.getProperty(RING_IS_AMULETTE, false);
			foreach (Lieu lieu in Lieux)
			{
				if (lieu.Amulette && !amulette) continue;

				if (lieu != Lieux[0] && arg.Text.ToLower().IndexOf(lieu.Name.ToLower()) != -1)
				{
					player.TempProperties.setProperty(RING_TARGET, lieu);
					player.Out.SendMessage("Votre demande a été entendue.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					return;
				}
			}
		}

		#region Timer, Item
		public static void ItemEquipped(DOLEvent e, object sender, EventArgs args)
		{
			ItemEquippedArgs arg = (ItemEquippedArgs)args;
			if (sender == null || (arg.Item.Id_nb != "dre_guarks_anneau" && arg.Item.Id_nb != "dre_guarks_amulette"))
				return;
			GamePlayer player = ((GamePlayerInventory)sender).Player;
			if (player.Client.ClientState != GameClient.eClientState.Playing)
				return;
			if (!CheckUse(player))
				return;

			player.Out.SendMessage("Vous ressentez une puissante magie qui vous immobilise.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			player.MaxSpeedBase = 1;
			player.Out.SendUpdateMaxSpeed();

			if (player.HealthPercent <= 75)
			{
				if (arg.Item.Id_nb == "anneau_guarks")
					player.Out.SendMessage("Vous n'avez pas assez d'énergie vitale pour utiliser l'anneau.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				else
					player.Out.SendMessage("Vous n'avez pas assez d'énergie vitale pour utiliser l'amulette.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return;
			}

			int harm = player.MaxHealth / 10; // 10%
			if (harm < player.Health)
				player.Health -= harm;
			else
			{
				player.Health = 0;
				player.Die(player);
				return;
			}

			_playerIDs.Add(player.InternalID);

			RegionTimer timer = new RegionTimer(player, TimerTicks);
			timer.Properties.setProperty("X", player.X);
			timer.Properties.setProperty("Y", player.Y);
			timer.Properties.setProperty("Z", player.Z);
			player.TempProperties.setProperty(RING_IS_AMULETTE, (arg.Item.Id_nb == "dre_guarks_amulette"));
			player.TempProperties.setProperty(RING_TIMER, timer);
			timer.Start(1000);


			foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				plr.Out.SendSpellEffectAnimation(player, player, 2661, 0, false, 1);
		}

		public static void ItemUnequipped(DOLEvent e, object sender, EventArgs args)
		{
			ItemUnequippedArgs arg = (ItemUnequippedArgs)args;
			if (sender == null || (arg.Item.Id_nb != "dre_guarks_anneau" && arg.Item.Id_nb != "dre_guarks_amulette"))
				return;
			GamePlayer player = ((GamePlayerInventory)sender).Player;
			if (player.Client.ClientState != GameClient.eClientState.Playing)
				return;

			if (player.MaxSpeedBase == 1)
			{
				player.MaxSpeedBase = 191;
				player.Out.SendUpdateMaxSpeed();
			}

			_playerIDs.Remove(player.InternalID);
			var timer = player.TempProperties.getProperty<RegionTimer>(RING_TIMER, null);
			if (timer != null)
				timer.Stop();
			player.TempProperties.removeProperty(RING_IS_AMULETTE);
			player.TempProperties.removeProperty(RING_TIMER);
			player.TempProperties.removeProperty(RING_TARGET);
		}

		public static int TimerTicks(RegionTimer timer)
		{
			var player = timer.Owner as GamePlayer;
			if (player == null)
			{
				timer.Stop();
				return 0;
			}
			//Vérification des mouvement, combats et si on stop le TP
			bool stop = !_playerIDs.Contains(player.InternalID);
			if (!stop)
				stop = !CheckUse(player);
			int x = timer.Properties.getProperty("X", 0);
			int y = timer.Properties.getProperty("Y", 0);
			int z = timer.Properties.getProperty("Z", 0);
			if (stop || player.InCombat || player.X != x || player.Y != y || player.Z != z)
			{
				timer.Stop();
				player.TempProperties.removeProperty(RING_TARGET);
				if (!stop)
					player.Out.SendMessage("Vous avez bougé, la téléportation est annulée.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				else if (!_playerIDs.Contains(player.InternalID) && !player.TempProperties.getProperty(RING_IS_AMULETTE, false))
					player.Out.SendMessage("Vous avez retiré l'anneau de votre doigt, la téléportation est annulée.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				else if (!_playerIDs.Contains(player.InternalID))
					player.Out.SendMessage("Vous avez retiré l'amulette de votre cou, la téléportation est annulée.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				_playerIDs.Remove(player.InternalID);
				return 0;
			}

			int ticks = timer.Properties.getProperty("ticks", 0);
			ticks += 1000;
			timer.Properties.setProperty("ticks", ticks);

			int harm = player.MaxHealth / 10; // 10%
			switch (ticks)
			{
				case 1000:
				case 3000:
				case 5000:
					if (harm < player.Health)
						player.Health -= harm;
					else
					{
						player.Health = 0;
						player.Die(player);
						timer.Stop();
						_playerIDs.Remove(player.InternalID);
						player.TempProperties.removeProperty(RING_TARGET);
						return 0;
					}
					break;

				case 7000:
					harm /= 2; // 5%
					if (harm < player.Health)
						player.Health -= harm;
					else
					{
						player.Health = 0;
						player.Die(player);
						timer.Stop();
						_playerIDs.Remove(player.InternalID);
						player.TempProperties.removeProperty(RING_TARGET);
						return 0;
					}
					break;

				case 2000: //2s
					if (harm < player.Health)
						player.Health -= harm;
					else
					{
						player.Health = 0;
						player.Die(player);
						timer.Stop();
						_playerIDs.Remove(player.InternalID);
						player.TempProperties.removeProperty(RING_TARGET);
						return 0;
					}

					foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						plr.Out.SendSpellEffectAnimation(player, player, 2661, 0, false, 1);
					break;

				case 4000: //4s
					if (harm < player.Health)
						player.Health -= harm;
					else
					{
						player.Health = 0;
						player.Die(player);
						timer.Stop();
						_playerIDs.Remove(player.InternalID);
						player.TempProperties.removeProperty(RING_TARGET);
						return 0;
					}

					foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						plr.Out.SendSpellEffectAnimation(player, player, 2661, 0, false, 1);
						plr.Out.SendSpellEffectAnimation(player, player, 1677, 0, false, 1);
					}
					break;

				case 6000: //6s
					if (harm < player.Health)
						player.Health -= harm;
					else
					{
						player.Health = 0;
						player.Die(player);
						timer.Stop();
						_playerIDs.Remove(player.InternalID);
						player.TempProperties.removeProperty(RING_TARGET);
						return 0;
					}

					foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						plr.Out.SendSpellEffectAnimation(player, player, 82, 0, false, 1);
						plr.Out.SendSpellEffectAnimation(player, player, 276, 0, false, 1);
					}
					break;

				case 8000: //8s
					foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						plr.Out.SendSpellEffectAnimation(player, player, 2569, 0, false, 1);
					break;

				case 9000: //9s
					Lieu lieu = player.TempProperties.getProperty<Lieu>(RING_TARGET, null);
					if (Util.Chance(5))
					{
						lieu = Lieux[0];
						player.Out.SendMessage("La magie céleste a échoué, destination inconnue.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					}
					else if (lieu == null)
					{
						lieu = GetRandomLieu(player.TempProperties.getProperty(RING_IS_AMULETTE, false));
						player.Out.SendMessage("Aucune demande n'a été formulée, la destination est inconnue.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					}

					ushort oldRegion = player.CurrentRegionID;
					player.MoveTo(lieu.Region, lieu.X, lieu.Y, lieu.Z, lieu.Heading);
					if (lieu.Region == oldRegion)
						new RegionTimer(player, EffectCallback, 500);
					else
						GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, EnterWorld);

					timer.Stop();
					_playerIDs.Remove(player.InternalID);
					player.TempProperties.removeProperty(RING_TARGET);
					return 0;
			}

			return 1000;
		}
		#endregion

		#region Methodes
		private static bool CheckUse(GamePlayer player)
		{
			if (player.InCombat)
			{
				player.Out.SendMessage("Vous ne pouvez pas utiliser la magie céleste en combat !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (RvrManager.Instance.IsInRvr(player) || PvpManager.Instance.IsIn(player))
			{
				player.Out.SendMessage("La magie céleste n'éxiste pas en ce lieu.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (JailMgr.IsPrisoner(player))
			{
				player.Out.SendMessage("Vous ne pouvez pas utiliser la magie céleste en prison !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (player.IsRiding)
			{
				player.Out.SendMessage("Vous ne pouvez pas utiliser la magie céleste à cheval !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return false;
			}
			return true;
		}

		private static void EnterWorld(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(sender, GamePlayerEvent.RegionChanged, EnterWorld);

			if (!(sender is GamePlayer player))
				return;
			new RegionTimer(player, EffectCallback, 500);
		}

		private static int EffectCallback(RegionTimer timer)
		{
			var player = timer.Owner as GamePlayer;
			if (player != null)
				foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					plr.Out.SendSpellEffectAnimation(player, player, 276, 0, false, 1);
			timer.Stop();
			return 0;
		}
		#endregion

		#region RandomLieu/Lieu
		private static Lieu GetRandomLieu(bool amulette)
		{
			if (amulette)
				return Lieux[Util.Random(0, Lieux.Length - 1)];

			int i = 0;
			foreach (Lieu lieu in Lieux)
				if (!lieu.Amulette)
					i++;
			int random = Util.Random(1, i);
			i = 0;
			foreach (Lieu lieu in Lieux)
				if (!lieu.Amulette)
				{
					i++;
					if (i == random)
						return lieu;
				}
			return null;
		}

		public class Lieu
		{
			public string Name;
			public int X;
			public int Y;
			public int Z;
			public ushort Heading;
			public ushort Region;
			public bool Amulette;

			public Lieu(string name, int x, int y, int z, ushort heading, ushort region, bool amulette)
			{
				Name = name;
				X = x;
				Y = y;
				Z = z;
				Heading = heading;
				Region = region;
				Amulette = amulette;
			}
		}
		#endregion
	}
}
