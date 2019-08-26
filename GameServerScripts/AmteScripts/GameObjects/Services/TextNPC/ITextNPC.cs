/**
 * Created by Virant "Dre" Jérémy for Amtenael
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Scripts
{
    public interface ITextNPC
    {
        TextNPCPolicy TextNPCData { get; set; }
        void SayRandomPhrase();
    }

    public class TextNPCPolicy
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private long _lastPhrase;
        private readonly GameNPC _body;

        public readonly Dictionary<string, DBEchangeur> EchangeurDB = new Dictionary<string, DBEchangeur>();
        public Dictionary<string, string> Reponses { get; private set; }
        public Dictionary<string, eEmote> EmoteReponses { get; private set; }
        public Dictionary<string, ushort> SpellReponses { get; private set; }
        public Dictionary<string, eEmote> RandomPhrases { get; private set; }
        public string Interact_Text { get; set; }
        public int PhraseInterval { get; set; }
        public TextNPCCondition Condition { get; private set; }
        public DBTextNPC TextDB { get; set; }

        public TextNPCPolicy(GameNPC body)
        {
            Condition = new TextNPCCondition("");
            Reponses = new Dictionary<string, string>();
            EmoteReponses = new Dictionary<string, eEmote>();
            SpellReponses = new Dictionary<string, ushort>();
            _body = body;
            _lastPhrase = 0;
            Interact_Text = "";
            PhraseInterval = 0;
        }

        public bool Interact(GamePlayer player)
        {
            if (string.IsNullOrEmpty(Interact_Text) || !CheckAccess(player))
                return false;

            _body.TurnTo(player);

            //Message
            string text = string.Format(Interact_Text, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
            if (text != "")
                player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);

            //Spell
            if (SpellReponses != null && SpellReponses.ContainsKey("INTERACT"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendSpellEffectAnimation(_body, player, SpellReponses["INTERACT"], 0, false, 1);

            //Emote
            if (EmoteReponses != null && EmoteReponses.ContainsKey("INTERACT"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, EmoteReponses["INTERACT"]);

            return true;
        }

        public bool WhisperReceive(GameLiving source, string str)
        {
            if (!(source is GamePlayer))
                return false;
            GamePlayer player = source as GamePlayer;
            if (!CheckAccess(player))
                return false;

            _body.TurnTo(player, 10000);

            //Message
            if (Reponses != null && Reponses.ContainsKey(str))
            {
                string text = string.Format(Reponses[str], player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                if (text != "")
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            //Spell
            if (SpellReponses != null && SpellReponses.ContainsKey(str))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendSpellEffectAnimation(_body, player, SpellReponses[str], 0, false, 1);

            //Emote
            if (EmoteReponses != null && EmoteReponses.ContainsKey(str))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, EmoteReponses[str]);


            return true;
        }

        public bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (!(source is GamePlayer) || item == null || !EchangeurDB.ContainsKey(item.Id_nb))
                return false;

            GamePlayer player = source as GamePlayer;
            if (!CheckAccess(player)) return false;
            _body.TurnTo(player);

            DBEchangeur EchItem = EchangeurDB[item.Id_nb];
            if (EchItem.ItemRecvCount > item.Count)
            {
                player.Out.SendMessage("Vous devez donner " + EchItem.ItemRecvCount + " " + item.Name + ".", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (!player.Inventory.RemoveCountFromStack(item, EchItem.ItemRecvCount))
                return false;
            InventoryLogging.LogInventoryAction(player, _body, eInventoryActionType.Quest, item.Template, EchItem.ItemRecvCount);

            if (EchItem.GiveTemplate != null)
                if (!player.Inventory.AddTemplate(GameInventoryItem.Create(EchItem.GiveTemplate), EchItem.ItemGiveCount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                {
                    player.Out.SendMessage("Votre inventaire est plein, l'objet est déposé au sol.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    for (int i = 0; i < EchItem.ItemGiveCount; i++)
                        player.CreateItemOnTheGround(GameInventoryItem.Create(EchItem.GiveTemplate));
                }
                else
                    InventoryLogging.LogInventoryAction(_body, player, eInventoryActionType.Quest, EchItem.GiveTemplate, EchItem.ItemGiveCount);

            if (EchItem.GainMoney > 0)
            {
                player.AddMoney(EchItem.GainMoney);
                InventoryLogging.LogInventoryAction(_body, player, eInventoryActionType.Quest, EchItem.GainMoney);
            }
            if (EchItem.GainXP > 0)
                player.GainExperience(GameLiving.eXPSource.Quest, EchItem.GainXP);
            else if (EchItem.GainXP < 0)
            {
                long xp = (player.ExperienceForNextLevel - player.ExperienceForCurrentLevel ) * EchItem.GainXP / -1000;
                player.GainExperience(GameLiving.eXPSource.Quest, xp);
            }

            EchItem.ChangedItemCount++;
            GameServer.Database.SaveObject(EchItem);

            if (Reponses != null && Reponses.ContainsKey(EchItem.ItemRecvID))
            {
                string text = string.Format(Reponses[EchItem.ItemRecvID], player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                if (text != "")
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return true;
        }

        public bool CheckAccess(GamePlayer player)
        {
            return Condition.CheckAccess(player);
        }

        public virtual void SayRandomPhrase()
        {
            if (RandomPhrases == null || RandomPhrases.Count < 1)
                return;
            if (_lastPhrase > DateTime.Now.Ticks / 10000 - PhraseInterval * 1000)
                return;
            _lastPhrase = DateTime.Now.Ticks / 10000;

            //Heure
            int heure = (int)(WorldMgr.GetCurrentGameTime() / 1000 / 60 / 54);
            if (Condition.Heure_max < Condition.Heure_min && (Condition.Heure_min > heure || heure <= Condition.Heure_max))
                return;
            if (Condition.Heure_max > Condition.Heure_min && (Condition.Heure_min > heure || heure >= Condition.Heure_max))
                return;
            if (Condition.Heure_max == Condition.Heure_min && heure != Condition.Heure_min)
                return;

            int phrase = Util.Random(0, RandomPhrases.Count - 1);
            int i = 0;
            string text = "";
            eEmote emote = 0;
            foreach (var de in RandomPhrases)
            {
                if (i == phrase)
                {
                    text = de.Key;
                    emote = de.Value;
                    break;
                }
                i++;
            }

            //Phrase
            if (text.StartsWith("say:"))
                _body.Say(text.Substring(4));
            else if (text.StartsWith("yell:"))
                _body.Yell(text.Substring(5));
            else if (text.StartsWith("em:"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.YELL_DISTANCE))
                    plr.Out.SendMessage(_body.Name + " " + text.Substring(3), eChatType.CT_Emote, eChatLoc.CL_ChatWindow);

            //Emote
            if (emote != 0)
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, emote);

        }

        public void LoadFromDatabase(DataObject obj)
        {
            var objs = GameServer.Database.SelectObjects<DBEchangeur>("`NpcID` = '" + GameServer.Database.Escape(obj.ObjectId) + "'");
            foreach (DBEchangeur echangeur in objs)
                if (!EchangeurDB.ContainsKey(echangeur.ItemRecvID))
                    EchangeurDB.Add(echangeur.ItemRecvID, echangeur);

            DBTextNPC data = null;
            try
            {
                data = GameServer.Database.SelectObject<DBTextNPC>("MobID = '" + GameServer.Database.Escape(obj.ObjectId) + "'");
            }
            catch
            {
                DBTextNPC.Init();
            }
            if (data == null)
                data = GameServer.Database.SelectObject<DBTextNPC>("MobID = '" + GameServer.Database.Escape(obj.ObjectId) + "'");
            if (data == null)
                return;

            //Chargement des textes
            TextDB = data;
            Interact_Text = TextDB.Text;
            var table = new Dictionary<string, string>();
            if (TextDB.Reponse != "")
            {
                foreach (string item in TextDB.Reponse.Split(';'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    table.Add(items[0], items[1]);
                }
            }
            Reponses = table;

            //Chargement des spells réponses
            var table2 = new Dictionary<string, ushort>();
            if (TextDB.ReponseSpell != "")
            {
                foreach (string item in TextDB.ReponseSpell.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        table2.Add(items[0], ushort.Parse(items[1]));
                    }
                    catch { }
                }
            }
            SpellReponses = table2;

            //Chargement des emotes réponses
            var table3 = new Dictionary<string, eEmote>();
            if (TextDB.ReponseEmote != "")
            {
                foreach (string item in TextDB.ReponseEmote.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        table3.Add(items[0], (eEmote)Enum.Parse(typeof(eEmote), items[1], false));
                    }
                    catch { }
                }
            }
            EmoteReponses = table3;

            //phrase/emote aléatoire
            var table4 = new Dictionary<string, eEmote>();
            if (TextDB.RandomPhraseEmote != "")
            {
                foreach (string item in TextDB.RandomPhraseEmote.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        if (items[1] == "0")
                            table4.Add(items[0], 0);
                        else
                            table4.Add(items[0], (eEmote)Enum.Parse(typeof(eEmote), items[1], false));
                    }
                    catch (Exception e)
                    {
                        log.Error("ERROR :", e);
                    }
                }
            }
            RandomPhrases = table4;
            PhraseInterval = TextDB.PhraseInterval;


            //Chargement des conditions
            Condition = new TextNPCCondition(TextDB.Condition);
        }

        public void SaveIntoDatabase()
        {
            if (TextDB == null)
                TextDB = new DBTextNPC();
            TextDB.MobID = _body.InternalID;
            TextDB.MobName = _body.Name;
            TextDB.MobRealm = (byte)_body.Realm;
            TextDB.Text = Interact_Text;

            //Sauve les réponses
            string reponse = "";
            if (Reponses != null && Reponses.Count > 0)
            {
                foreach (var de in Reponses)
                {
                    if (reponse.Length > 1)
                        reponse += ";";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value.Trim('|', ';');
                }
            }
            TextDB.Reponse = reponse;

            //Sauve les spell réponses
            reponse = "";
            if (SpellReponses != null && SpellReponses.Count > 0)
            {
                foreach (var de in SpellReponses)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value;
                }
            }
            TextDB.ReponseSpell = reponse;

            //Sauve les emote réponses
            reponse = "";
            if (EmoteReponses != null && EmoteReponses.Count > 0)
            {
                foreach (var de in EmoteReponses)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value;
                }
            }
            TextDB.ReponseEmote = reponse;

            //Sauve les phrase/emote aléatoire
            reponse = "";
            if (RandomPhrases != null && RandomPhrases.Count > 0)
            {
                foreach (var de in RandomPhrases)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', '\n') + "|" + de.Value;
                }
            }
            TextDB.RandomPhraseEmote = reponse;
            TextDB.PhraseInterval = PhraseInterval;

            //Sauve les conditions
            if (Condition != null)
                TextDB.Condition = Condition.GetConditionString();

            DBTextNPC data = GameServer.Database.SelectObject<DBTextNPC>("MobID = '" + GameServer.Database.Escape(_body.InternalID) + "'");
            if (data == null)
                GameServer.Database.AddObject(TextDB);
            else
            {
                TextDB.ObjectId = data.ObjectId;
                GameServer.Database.SaveObject(TextDB);
            }

            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
            {
                pair.Value.NpcID = _body.InternalID;
                if (pair.Value.IsPersisted)
                    GameServer.Database.SaveObject(pair.Value);
                else
                    GameServer.Database.AddObject(pair.Value);
            }
        }

        public void DeleteFromDatabase()
        {
            if (TextDB != null)
                GameServer.Database.DeleteObject(TextDB);
            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
                GameServer.Database.DeleteObject(pair.Value);
        }

        public IList<string> DelveInfo()
        {
            List<string> text = new List<string>
				{
					" + OID: " + _body.ObjectID,
					" + Class: " + _body.GetType(),
					" + Position: X=" + _body.X + " Y=" + _body.Y + " Z=" + _body.Z + " Heading=" + _body.Heading,
					" + Realm: " + _body.Realm,
					" + Model: " + _body.Model,
					"",
					"-- Echangeur (Items) --"
				};
            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
            {
                text.Add(" - " + pair.Value.ItemRecvID + " (" + pair.Value.ItemRecvCount + "):");
                if (pair.Value.ItemGiveCount > 0)
                    text.Add(" . " + pair.Value.ItemGiveCount + " " + pair.Value.ItemGiveID);
                if (pair.Value.GainMoney > 0)
                    text.Add(" . " + Money.GetString(pair.Value.GainMoney));
                if (pair.Value.GainXP > 0)
                    text.Add(" . " + pair.Value.GainXP + "xp");
                if (pair.Value.GainXP < 0)
                    text.Add(" . " + (-pair.Value.GainXP) + "/1000 du niveau en cours");
                if (Reponses.ContainsKey(pair.Value.ItemRecvID))
                    text.Add(" . Réponse: " + Reponses[pair.Value.ItemRecvID]);
                text.Add(" . " + pair.Value.ChangedItemCount + " Items échangés");
            }
            return text;
        }
    }
}
