using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
	public class FretNPC : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public const string REPERTOIRE = "./logs/fret/"; //Répertoire des logs
		public const int MaxItem = 30;

		private static readonly Dictionary<string, InteractPlayer> TempItems = new Dictionary<string, InteractPlayer>();

		[GameServerStartedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			GameServer.Database.RegisterDataObject(typeof(DBFret));
			log.Info("DATABASE Fret LOADED");

			GameEventMgr.AddHandler(GamePlayerEvent.Quit, (ev, s, a) =>
				{
					GamePlayer p = s as GamePlayer;
					if (p != null)
						TempItems.Remove(p.InternalID);
				}
			);
		}

		public FretNPC()
		{
			SetOwnBrain(new BlankBrain());
		}

		/// <summary>
		/// Intéraction avec le PNJ
		/// </summary>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			TurnTo(player);
			AmteUtils.SendClearPopupWindow(player);

			if (TempItems.ContainsKey(player.InternalID))
				return Recapitulatif(player);

			IList<DBFret> ItemsFret = GameServer.Database.SelectObjects<DBFret>("`ToPlayer` = '" + GameServer.Database.Escape(player.InternalID) + "'");

			if (ItemsFret == null || ItemsFret.Count <= 0)
			{
				player.Out.SendMessage("Bonjour " + player.Name + ", je n'ai aucun colis à votre nom !\n" +
									   "\nSi vous voulez envoyer un objet à une personne, donnez le moi et chuchotez-moi (/whisper <nom>) le nom de la personne.",
									   eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			else
			{
				string message = "Bonjour " + player.Name + ", j'ai " + ItemsFret.Count + " colis à votre nom:\n\n";
				int id = 1;
				foreach (DBFret fret in ItemsFret)
				{
					message += " [" + id + "] " + fret.Name + " donné par " + fret.FromPlayer + "\n";
					id++;
					if (message.Length > 1900)
					{
						player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
						message = "";
					}
				}
				message += "\n(pour récupérer un colis, cliquez juste sur son numéro)";
				player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			return true;
		}

		/// <summary>
		/// Le pnj reçoit un /whisper (ou clic sur du texte entre [])
		/// </summary>
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str) || !(source is GamePlayer)) return false;
			TurnTo(source);
			GamePlayer player = source as GamePlayer;
			AmteUtils.SendClearPopupWindow(player);

			#region Réception d'un item
			int id;
			if (int.TryParse(str, out id))
			{
				IList<DBFret> ItemsFret = GameServer.Database.SelectObjects<DBFret>("`ToPlayer` = '" + GameServer.Database.Escape(player.InternalID) + "'");
				string msg;
				try
				{
					DBFret item = ItemsFret[id - 1];
					if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(item)))
					{
						GameServer.Database.DeleteObject(item);
						if (item.Template is ItemUnique)
							GameServer.Database.AddObject(item.Template);
						msg = "Vous récupérez " + (item.Count > 1 ? item.Count.ToString() : "") + item.Name +
							  " donné par " + item.FromPlayer + " !";
					}
					else msg = "Vérifiez que votre sac à dos n'est pas plein !";
				}
				catch
				{
					msg = "Un problème est survenu, recommencez la manipulation pour récupérer votre objet.";
				}

				player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			#endregion
			#region Envoi du colis
			else if (str == "Envoyer le colis")
			{
				if (TempItems.ContainsKey(player.InternalID))
				{
					InteractPlayer IP = TempItems[player.InternalID];
					player.Out.SendCustomDialog("Voulez-vous envoyer votre colis pour " + Money.GetString(IP.Price) + " ?",
												SendColisResponse);
				}
				else
					player.Out.SendMessage("Vous devez préparer un colis avant de l'envoyer !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			#endregion
			#region Réglage du destinataire
			else if (TempItems.ContainsKey(player.InternalID) && str.Split(' ').Length == 1)
			{
				//On récupére le joueur existant à ce nom:
				DOLCharacters ch = GameServer.Database.SelectObject<DOLCharacters>("`Name` LIKE '" + GameServer.Database.Escape(str) + "'");
				string msg;
				if (ch == null)
					msg = "Je ne trouve pas de personne nommée " + str + ", vérifiez le nom.";
				else
				{
					msg = "Votre colis est pour " + ch.Name + ".";
					TempItems[player.InternalID].ToPlayerAccountName = ch.AccountName;
					TempItems[player.InternalID].ToPlayerID = ch.ObjectId;
					TempItems[player.InternalID].ToPlayerName = ch.Name;
				}
				player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
			#endregion
			else return false;
			return true;
		}

		/// <summary>
		/// Le pnj reçoit un item
		/// </summary>
		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			if (!(source is GamePlayer)) return false;
			if (!item.IsTradable || item.IsDeleted) return false;
			GamePlayer player = source as GamePlayer;
			AmteUtils.SendClearPopupWindow(player);
			AddRemoveItem(player, item);
			return false;
		}

		#region Creation Colis
		/// <summary>
		/// Affiche les informations du colis
		/// </summary>
		private static bool Recapitulatif(GamePlayer player)
		{
			InteractPlayer IP = TempItems[player.InternalID];
			string msg = "Vous vous préparez à envoyer ce colis :\n";
			msg += " - Pour: " + (IP.ToPlayerID == "" ? "(Non spécifié, tapez '/whisper <nom>' pour le donner)" : IP.ToPlayerName) + "\n";
			msg += " - Poids: " + (IP.Weight / 10) + "," + (IP.Weight % 10) + "livres\n";
			msg += " - Prix: " + Money.GetString(IP.Price) + "\n";
			msg += " - Objets: " + IP.Items.Count + "/" + MaxItem + "\n";
			int id = 1;
			foreach (InventoryItem item in IP.Items)
			{
				msg += id + ". " + (item.Count > 1 ? item.Count + " " : "") + item.Name + "\n";
				id++;
			}
			msg += "\nPour retirer un objet, donnez le même objet une deuxième fois.\n[Envoyer le colis]";
			player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		/// <summary>
		/// Ajoute ou retire un objet du colis
		/// </summary>
		private static void AddRemoveItem(GamePlayer player, InventoryItem item)
		{
			InteractPlayer IP;
			if (TempItems.ContainsKey(player.InternalID))
				IP = TempItems[player.InternalID];
			else
			{
				IP = new InteractPlayer(player);
				TempItems.Add(player.InternalID, IP);
			}
			if (IP.Items.Count >= MaxItem)
			{
				player.Out.SendMessage("Vous ne pouvez envoyer que " + MaxItem + " objets dans un colis.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return;
			}
			if (IP.Items.Contains(item))
			{
				IP.RemoveItem(item);
				player.Out.SendMessage("L'objet \"" + item.Name + "\" a été retiré des objets du colis.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return;
			}
			IP.AddItem(item);
			player.Out.SendMessage("L'objet \"" + item.Name + "\" a été ajouté aux autres objets du colis.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}

		/// <summary>
		/// Réponse à la question de confirmation de l'envoi du colis
		/// </summary>
		private static void SendColisResponse(GamePlayer player, byte response)
		{
			if (response != 0x01) return;
			InteractPlayer IP = TempItems[player.InternalID];
			if (player.GetCurrentMoney() >= IP.Price)
			{
				if (SendColis(player)) player.RemoveMoney(IP.Price);
			}
			else
				player.Out.SendMessage("Vous n'avez pas assez d'argent !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}

		/// <summary>
		/// Envoi le colis
		/// </summary>
		private static bool SendColis(GamePlayer player)
		{
			if (!TempItems.ContainsKey(player.InternalID))
			{
				player.Out.SendMessage("Vous n'avez pas préparé de colis !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}
			InteractPlayer IP = TempItems[player.InternalID];
			if (IP.ToPlayerID == "" || IP.Items.Count <= 0)
			{
				Recapitulatif(player);
				return false;
			}
			List<DBFret> frets = new List<DBFret>(IP.Items.Count);
			List<InventoryItem> Sended = new List<InventoryItem>(IP.Items.Count);
			bool ok = true;
			foreach (InventoryItem item in IP.Items)
			{
				if (player.Inventory.RemoveCountFromStack(item, item.Count))
				{
					frets.Add(new DBFret(item, IP.ToPlayerID, player.Name));
					Sended.Add(item);
					try
					{
						//Log
						FretLog.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + IP.Player.Name + " ("
										  + IP.Player.Client.Account.Name + ") => " + IP.ToPlayerName + " ("
										  + IP.ToPlayerID + "): " + item.Count + " " + item.Name + " (" + item.Id_nb
										  + ")");
						//string objectid = (item.Id_nb == "BANQUE_CHEQUE" ? Money.GetMoney(0, item.MaxDurability, item.MaxCondition, 0, item.Condition).ToString() : item.ObjectId);
						//GameServer.Instance.LogTradeAction("[FRET] " + IP.Player.Name + " (" + IP.Player.Client.Account.Name + ") -> " + IP.ToPlayerName + " (" + IP.ToPlayerAccountName + "): [ITEM] " + item.Count + " '" + item.Id_nb + "' (" + objectid + ")", 1);
						InventoryLogging.LogInventoryAction(player, "(FRET;" + IP.ToPlayerName + ")", eInventoryActionType.Trade, item.Template, item.Count);
					}
					catch (Exception e)
					{
						log.Error("FretLog error: ", e);
					}
				}
				else
				{
					ok = false;
					break;
				}
			}
			//On enregistre l'envoi du colis
			if (!ok)
			{
				foreach (InventoryItem item in Sended)
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
				TempItems.Remove(player.InternalID);
				player.Out.SendMessage("Un problème est survenu lors de l'envoi du colis, veuillez tout recommencer. (Ne déplacez pas vos objets pendant la création du colis)", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}
			foreach (DBFret fret in frets)
			{
				if (fret.Template is ItemUnique)
					GameServer.Database.AddObject(fret.Template);
				GameServer.Database.AddObject(fret);
			}
			TempItems.Remove(player.InternalID);
			player.Out.SendMessage("Votre colis a été envoyé !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}
		#endregion

		#region InteractPlayer
		/// <summary>
		/// Classe du joueur qui intéragit avec le pnj et gestion du colis en cours
		/// </summary>
		public class InteractPlayer
		{
			public GamePlayer Player;
			public List<InventoryItem> Items;
			public int Weight;
			public long Price;
			public string ToPlayerID;
			public string ToPlayerName;
			public string ToPlayerAccountName;

			/// <summary>
			/// Constructeur
			/// </summary>
			public InteractPlayer(GamePlayer pl)
			{
				Items = new List<InventoryItem>();
				Weight = 0;
				Price = 0;
				ToPlayerID = "";
				ToPlayerName = "";
				Player = pl;
			}

			/// <summary>
			/// Ajoute un item au colis
			/// </summary>
			public void AddItem(InventoryItem it)
			{
				Items.Add(it);
				Weight += it.Weight;
				CalculPrice();
			}

			/// <summary>
			/// Retire item du colis
			/// </summary>
			public bool RemoveItem(InventoryItem it)
			{
				if (!Items.Remove(it)) return false;
				Weight -= it.Weight;
				CalculPrice();
				return true;
			}

			/// <summary>
			/// Recalcule le prix de l'envoi du colis
			/// </summary>
			private void CalculPrice()
			{
				//Minimum 10pc l'envoi, 1pa/livre jusqu'à 20livres, 50pc/livre si plus
				Price = 100 * Math.Min(Weight, 200);
				if (Weight > 200)
					Price += 50 * (Weight - 200);
				if (Price < 100) Price = 10;
			}
		}
		#endregion

		#region Log
		private static Timer DateChangeTimer;
		private static StreamWriter FretLog;

		/// <summary>
		/// Initialisation du log
		/// </summary>
		[GameServerStartedEvent]
		public static void Init(DOLEvent e, object sender, EventArgs args)
		{
			string file = "Fretlog_" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;

			if (!Directory.Exists(REPERTOIRE))
				Directory.CreateDirectory(REPERTOIRE);
			if (REPERTOIRE.EndsWith("\\"))
				file = REPERTOIRE + file;
			else
				file = REPERTOIRE + "\\" + file;

			try
			{
				FretLog = new StreamWriter(file + ".log", true) { AutoFlush = true };
			}
			catch
			{
				try
				{
					FretLog = new StreamWriter(file + "_1.log", true) { AutoFlush = true };
				}
				catch
				{
					log.Error("Can't create log fret");
				}
			}

			long wait = 864000000000 + (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 1).Ticks - DateTime.Now.Ticks);
			wait /= 10000; //En milliseconde
			DateChangeTimer = new Timer(DateChange, null, wait, 86400000);
		}

		private static void DateChange(object obj)
		{
			string file = "Fretlog_" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;

			if (!Directory.Exists(REPERTOIRE))
				Directory.CreateDirectory(REPERTOIRE);
			if (REPERTOIRE.EndsWith("\\"))
				file = REPERTOIRE + file;
			else
				file = REPERTOIRE + "\\" + file;

			StreamWriter SW;
			try
			{
				SW = new StreamWriter(file + ".log", true) { AutoFlush = true };
			}
			catch
			{
				try
				{
					SW = new StreamWriter(file + "_1.log", true) { AutoFlush = true };
				}
				catch
				{
					log.Error("Can't create log fret");
					return;
				}
			}

			try
			{
				FretLog.Close();
				FretLog.Dispose();
				FretLog = SW;
			}
			catch (Exception e)
			{
				log.Error(" [AMT] Fret: Changement de jours (logs).", e);
			}
		}

		/// <summary>
		/// Fermeture du serveur
		/// </summary>
		[GameServerStoppedEvent]
		public static void Close(DOLEvent e, object sender, EventArgs args)
		{
			if (DateChangeTimer != null)
			{
				DateChangeTimer.Change(Timeout.Infinite, Timeout.Infinite);
				DateChangeTimer.Dispose();
				DateChangeTimer = null;
			}
			if (FretLog != null)
			{
				FretLog.Close();
				FretLog.Dispose();
			}
		}
		#endregion
	}
}
