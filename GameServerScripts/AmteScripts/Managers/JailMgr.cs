using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	/// <summary>
	/// Summary description for JailMgr.
	/// </summary>
	public static class JailMgr
	{
        public const ushort Radius = 300; //Taille de la prison
	    
		// Réglages prison
		// Bind de prison + tp
		public const int PrisonHRP_X = 32943;
		public const int PrisonHRP_Y = 34440;
		public const int PrisonHRP_Z = 16528;
		public const int PrisonHRP_RegionID = 499;
		public const int PrisonHRP_Heading = 1043;
		// Bind de sortie de prison + tp
        public const int SortieHRP_X = 434493;
        public const int SortieHRP_Y = 492983;
        public const int SortieHRP_Z = 3085;
        public const int SortieHRP_RegionID = 51;
        public const int SortieHRP_Heading = 3560;

		// Bind de prison HRP + tp
        public const int Prison_X = 434518;
        public const int Prison_Y = 494012;
        public const int Prison_Z = 3191;
        public const int Prison_RegionID = 51;
        public const int Prison_Heading = 3115;
		// Bind de sortie de prison HRP + tp
        public const int Sortie_X = 434493;
        public const int Sortie_Y = 492983;
        public const int Sortie_Z = 3085;
        public const int Sortie_RegionID = 51;
        public const int Sortie_Heading = 3560;

	    /// <summary>
	    /// Prisonniers
	    /// </summary>
        public static List<GamePlayer> Prisonniers = new List<GamePlayer>();

	    public static Dictionary<GamePlayer, Prisoner> PlayerXPrisoner = new Dictionary<GamePlayer, Prisoner>();

        [GameServerStartedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            Area.Circle prisHRP = new Area.Circle("", PrisonHRP_X, PrisonHRP_Y, PrisonHRP_Z, Radius);
            WorldMgr.GetRegion(PrisonHRP_RegionID).AddArea(prisHRP);
            
            Area.Circle prisRP = new Area.Circle("", Prison_X, Prison_Y, Prison_Z, Radius);
            WorldMgr.GetRegion(Prison_RegionID).AddArea(prisRP);

            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEnter));
            GameEventMgr.AddHandler(prisHRP, AreaEvent.PlayerLeave, PlayerEvade);
            GameEventMgr.AddHandler(prisRP, AreaEvent.PlayerLeave, PlayerEvade);
        }

		private static void PlayerEnter(DOLEvent e, object sender, EventArgs args)
		{
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
			Prisoner prison = GameServer.Database.SelectObject<Prisoner>("PlayerId = '" + GameServer.Database.Escape(player.InternalID) + "'");
            if (prison == null) return;
            if (prison.Sortie != DateTime.MinValue && prison.Sortie.Ticks <= DateTime.Now.Ticks)
            {
                Relacher(player);
                return;
            }
            Prisonniers.Add(player);
            PlayerXPrisoner.Add(player, prison);

            if (prison.RP) player.MoveTo(Prison_RegionID, Prison_X, Prison_Y, Prison_Z, Prison_Heading);
            else player.MoveTo(PrisonHRP_RegionID, PrisonHRP_X, PrisonHRP_Y, PrisonHRP_Z, PrisonHRP_Heading);

            player.MaxSpeedBase = 50;
            player.Out.SendUpdateMaxSpeed();

            player.TempProperties.setProperty("JailMgr", prison);
            GameEventMgr.AddHandler(player, GamePlayerEvent.Revive, PlayerRevive);
            GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, PlayerExit);
            GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, PlayerRevive);

            if (prison.Sortie == DateTime.MinValue) return;

			long time = (prison.Sortie.Ticks - DateTime.Now.Ticks) / 10000;
			if (time < 0)
				time = 1;
		    if (time >= 864000000) return;
		    RegionTimer SortieTimer = new RegionTimer(player, SortiePrison);
		    SortieTimer.Properties.setProperty("player", player);
		    SortieTimer.Start((int)time);
		}

        private static void PlayerRevive(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
        	Prisoner prison = player.TempProperties.getProperty<Prisoner>("JailMgr", null);
            if (prison == null) return;

            if (prison.RP) player.MoveTo(Prison_RegionID, Prison_X, Prison_Y, Prison_Z, Prison_Heading);
            else player.MoveTo(PrisonHRP_RegionID, PrisonHRP_X, PrisonHRP_Y, PrisonHRP_Z, PrisonHRP_Heading);
            player.Bind(true);

            if (player.MaxSpeed == 50) return;
            player.MaxSpeedBase = 50;
            player.Out.SendUpdateMaxSpeed();
        }
	    
	    private static void PlayerEvade(DOLEvent e, object sender, EventArgs args)
	    {
            GamePlayer player = ((AreaEventArgs)args).GameObject as GamePlayer;
            if (player == null || !Prisonniers.Contains(player)) return;
	        PlayerRevive(null, player, null);
	    }

        private static void PlayerExit(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
			Prisoner prison = GameServer.Database.SelectObject<Prisoner>("PlayerId = '" + GameServer.Database.Escape(player.InternalID) + "'");
            if (prison == null) return;
            Prisonniers.Remove(player);
            PlayerXPrisoner.Remove(player);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Revive, PlayerRevive);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, PlayerExit);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, PlayerRevive);
        }

        private static int SortiePrison(RegionTimer timer)
        {
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
            if (player == null || 
                (player.Client.ClientState != GameClient.eClientState.Playing &&
                player.Client.ClientState != GameClient.eClientState.WorldEnter))
                return 0;
            Relacher(player);
            return 0;
        }

		#region Animation
		private static void Animation(GameLiving player)
		{
			foreach(GamePlayer pl in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)) 
				pl.Out.SendSpellEffectAnimation(pl,player,177,0,false,1);
		}
		#endregion

		#region Entrée/Sortie Prison

		/// <summary>
		/// Emprisonner un joueur
		/// </summary>
		/// <param name="player">Prisonnier</param>
		/// <param name="cost">Amende</param>
		/// <param name="sortie">Date de sortie (si null ou valeur minimale alors temps infini)</param>
		/// <param name="GM"></param>
		/// <param name="JailRP"></param>
		/// <param name="raison"></param>
		private static void Emprisonner(GamePlayer player, int cost, DateTime sortie, string GM, bool JailRP, string raison)
		{
            //On vérifie le tps
            long time = (sortie.Ticks - DateTime.Now.Ticks) / 10000;
            if (sortie != DateTime.MinValue && time <= 0)
                return;

            //La DB
			Prisoner prisoner = new Prisoner(player)
			                      {
			                              Cost = cost,
			                              Sortie = sortie,
			                              RP = JailRP,
			                              Raison = raison
			                      };
		    GameServer.Database.AddObject(prisoner);
			if (JailRP)
				player.MoveTo(Prison_RegionID, Prison_X, Prison_Y, Prison_Z, Prison_Heading);
			else	player.MoveTo(PrisonHRP_RegionID, PrisonHRP_X, PrisonHRP_Y, PrisonHRP_Z, PrisonHRP_Heading);

			player.Bind(true);
			player.MaxSpeedBase = 50;
			player.Out.SendUpdateMaxSpeed();
            player.SaveIntoDatabase();
			Prisonniers.Add(player);
		    PlayerXPrisoner.Add(player, prisoner);

            //Les Events
            player.TempProperties.setProperty("JailMgr", prisoner);
			GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, PlayerExit);
            GameEventMgr.AddHandler(player, GamePlayerEvent.Revive, PlayerRevive);
            GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, PlayerRevive);
			Animation(player);

			if (JailRP)
				player.Out.SendMessage("Vous avez été mis en prison pour vos actes par "+ GM +". Votre caution s'éleve à "+cost+" Or. Pour sortir, demandez à quelqu'un de payer votre caution à Stronghold, le gardien de la prison.",eChatType.CT_Important,eChatLoc.CL_SystemWindow);
			else	player.Out.SendMessage("Vous avez été mis en prison pour HRP par "+ GM +". Vous devez attendre la fin de votre durée d'emprisonnement en temps réel pour sortir automatiquement.",eChatType.CT_Important,eChatLoc.CL_SystemWindow);

            if (sortie == DateTime.MinValue) return;

            //Timer pour sortir
		    if (sortie == DateTime.MinValue || time <= 0 || time >= 864000000) return;
		    RegionTimer SortieTimer = new RegionTimer(player, SortiePrison, 1);
		    SortieTimer.Properties.setProperty("player", player);
		    SortieTimer.Start((int)time);
		}

		/// <summary>
		/// Emprisonner un joueur non connecté
		/// </summary>
		/// <param name="playerName"></param>
		/// <param name="cost">Amende</param>
		/// <param name="sortie">Date de sortie (si null ou valeur minimale alors temps infini)</param>
		/// <param name="JailRP"></param>
		/// <param name="raison"></param>
		private static bool Emprisonner(string playerName, int cost, DateTime sortie, bool JailRP, string raison)
        {
            //On vérifie le tps
			long time = (sortie.Ticks - DateTime.Now.Ticks) / 10000;
            if (sortie != DateTime.MinValue && time <= 0)
                return false;
            
            //On vérifie si le joueur existe
        	DOLCharacters perso = GameServer.Database.SelectObject<DOLCharacters>("Name LIKE '" + GameServer.Database.Escape(playerName) + "'");
            if (perso == null || perso.Name.ToLower() != playerName.ToLower())
                return false;
            

            //La DB
            Prisoner Prisonnier = new Prisoner(perso)
                                  {
                                          Cost = cost,
                                          Sortie = sortie,
                                          RP = JailRP,
                                          Raison = raison
                                  };
            GameServer.Database.AddObject(Prisonnier);

            if (JailRP)
            {
                perso.Xpos = Prison_X;
                perso.Ypos = Prison_Y;
                perso.Zpos = Prison_Z;
                perso.Region = Prison_RegionID;

                perso.BindXpos = Prison_X;
                perso.BindYpos = Prison_Y;
                perso.BindZpos = Prison_Z;
                perso.BindHeading = Prison_Heading;
                perso.BindRegion = Prison_RegionID;
            }
            else
            {
                perso.Xpos = PrisonHRP_X;
                perso.Ypos = PrisonHRP_Y;
                perso.Zpos = PrisonHRP_Z;
                perso.Region = PrisonHRP_RegionID;

                perso.BindXpos = PrisonHRP_X;
                perso.BindYpos = PrisonHRP_Y;
                perso.BindZpos = PrisonHRP_Z;
                perso.BindHeading = PrisonHRP_Heading;
                perso.BindRegion = PrisonHRP_RegionID;
            }
            perso.MaxSpeed = 50;
            GameServer.Database.SaveObject(perso);

            return true;
        }

        /// <summary>
        /// Emprisonner un joueur connecté dans la prison RP
        /// </summary>
        public static void EmprisonnerRP(GamePlayer player, int cost, DateTime sortie, string GM, string raison) 
		{
            Emprisonner(player, cost, sortie, GM, true, raison);
		}
        /// <summary>
        /// Emprisonner un joueur connecté dans la prison HRP
        /// </summary>
        public static void EmprisonnerHRP(GamePlayer player, DateTime sortie, string GM, string raison) 
		{
            Emprisonner(player, 0, sortie, GM, false, raison);
		}

        /// <summary>
        /// Emprisonner un joueur déconnecté dans la prison RP
        /// </summary>
        public static bool EmprisonnerRP(string player, int cost, DateTime sortie, string GM, string raison)
        {
            return Emprisonner(player, cost, sortie, true, raison);
        }
        /// <summary>
        /// Emprisonner un joueur déconnecté dans la prison HRP
        /// </summary>
        public static bool EmprisonnerHRP(string player, DateTime sortie, string GM, string raison)
        {
            return Emprisonner(player, 0, sortie, false, raison);
        }

		/// <summary>
		/// Relâche un prisonnier
		/// </summary>
		/// <param name="player">Prisonnier</param>
		/// <returns></returns>
		public static bool Relacher(GamePlayer player)
		{
			Prisoner Prisonnier = GameServer.Database.SelectObject<Prisoner>("PlayerId = '" + GameServer.Database.Escape(player.InternalID) + "'");
			if(Prisonnier == null)
				return false;
			GameServer.Database.DeleteObject(Prisonnier);

			player.MaxSpeedBase = 191;
			player.Out.SendUpdateMaxSpeed();

            if (Prisonnier.RP) player.MoveTo(Sortie_RegionID, Sortie_X, Sortie_Y, Sortie_Z, Sortie_Heading);
            else player.MoveTo(SortieHRP_RegionID, SortieHRP_X, SortieHRP_Y, SortieHRP_Z, SortieHRP_Heading);
            player.Bind(true);
            player.SaveIntoDatabase();

			Prisonniers.Remove(player);
            PlayerXPrisoner.Remove(player);
            player.TempProperties.removeProperty("JailMgr");
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Revive, PlayerRevive);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, PlayerExit);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, PlayerRevive);
			Animation(player);
			return true;
		}

		/// <summary>
		/// Relâche un prisonnier
		/// </summary>
		/// <param name="player">Prisonnier</param>
		/// <returns></returns>
		public static bool Relacher(DOLCharacters player)
		{
            if (player == null)
                return false;
			Prisoner Prisonnier = GameServer.Database.SelectObject<Prisoner>("PlayerId = '" + GameServer.Database.Escape(player.ObjectId) + "'");
			if (Prisonnier == null)
				return false;
			GameServer.Database.DeleteObject(Prisonnier);

            if (Prisonnier.RP) //if(Sortie_X != 0 && Sortie_Y != 0 && Sortie_RegionID != 0 && Sortie_Heading != 0)
			{
				player.Region = Sortie_RegionID;
				player.Xpos = Sortie_X;
				player.Ypos = Sortie_Y;
				player.Zpos = Sortie_Z;
				player.BindRegion = Sortie_RegionID;
				player.BindXpos = Sortie_X;
				player.BindYpos = Sortie_Y;
				player.BindZpos = Sortie_Z;
				player.MaxSpeed = 191;
				GameServer.Database.SaveObject(player);
			}
			else 
			{
				player.Region = SortieHRP_RegionID;
				player.Xpos = SortieHRP_X;
				player.Ypos = SortieHRP_Y;
				player.Zpos = SortieHRP_Z;
				player.BindRegion = SortieHRP_RegionID;
				player.BindXpos = SortieHRP_X;
				player.BindYpos = SortieHRP_Y;
				player.BindZpos = SortieHRP_Z;
				player.MaxSpeed = 191;
				GameServer.Database.SaveObject(player);
			}
			return true;
		}

		/// <summary>
		/// Relâche un prisonnier
		/// </summary>
		/// <param name="Name">Nom du prisonnier</param>
		/// <returns></returns>
		public static bool Relacher(string Name)
		{
            GameClient client = null;
            if (WorldMgr.GetAllPlayingClientsCount() > 0)
                client = WorldMgr.GetClientByPlayerName(Name, true, true);
            if (client != null && client.Player != null)
                return Relacher(client.Player);
		    return Relacher(GameServer.Database.SelectObject<DOLCharacters>("Name = '" + GameServer.Database.Escape(Name) + "'"));
		}
		#endregion

        /// <summary>
        /// Donne l'entrée dans la base du prisonnier par son nom
        /// </summary>
        /// <param name="Name">Nom du prisonnier</param>
        /// <returns>Null si non trouvable</returns>
        public static Prisoner GetPrisoner(string Name)
        {
            Name = GameServer.Database.Escape(Name);
			return GameServer.Database.SelectObject<Prisoner>("Name = '" + Name + "'");
        }

		/// <summary>
		/// Retourne true si le joueur est un prisonnier.
		/// </summary>
		public static bool IsPrisoner(string Name)
		{
			return GetPrisoner(Name) != null;
		}

        /// <summary>
        /// Donne l'entrée dans la base du prisonnier par son nom
        /// </summary>
        /// <returns>Null si non trouvable</returns>
        public static Prisoner GetPrisoner(GamePlayer player)
        {
        	return player.TempProperties.getProperty<Prisoner>("JailMgr", null);
        }
		
        /// <summary>
        /// Retourne true si le joueur est un prisonnier.
        /// </summary>
        public static bool IsPrisoner(GamePlayer player)
        {
			return player.TempProperties.getProperty<Prisoner>("JailMgr", null) != null;
        }
	}
}
