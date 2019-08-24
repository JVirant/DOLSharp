using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Conditions:
    ///  - Heure (min - max)
    ///  - Level (min - max)
    ///  - guilde (interdites)
    ///  - guildeA (autorisées)
    ///  - race (interdites)
    ///  - classe (interdites)
    ///  - prp (min - max)
    /// </summary>
    public class TextNPCCondition
    {
        public int Level_min = 1;
        public int Level_max = 50;

        /// <summary>
        /// Noms de guilde interdits
        /// </summary>
        public List<string> GuildNames = new List<string>();
        /// <summary>
        /// Noms de guilde autorisées
        /// </summary>
        public List<string> GuildNamesA = new List<string>();
        /// <summary>
        /// Races interdites
        /// </summary>
        public List<string> Races = new List<string>();
        /// <summary>
        /// Classes interdites
        /// </summary>
        public List<string> Classes = new List<string>();

        public int Heure_min;
        public int Heure_max = 24;

        public bool CanGiveQuest = false;
    	public float Reput_min = BlacklistMgr.MinReputation;
		public float Reput_max = BlacklistMgr.MaxReputation;


    	/// <summary>
        /// Parse la variable cond
        /// </summary>
        /// <param name="cond">Texte à parser, exemple:
        /// level/10/50
        /// guild/Legion Noire/Caste des Chasseurs
        /// race/Troll/Elf
        /// class/Animist/Paladin
        /// prp/1/200</param>
        public TextNPCCondition(string cond)
        {
            GuildNamesA.Add("ALL");
            #region Parse condition
            if (!string.IsNullOrEmpty(cond))
            {
                foreach (string txt in cond.Split('\n'))
                {
                    if (txt == "") continue;
                    string[] condition = txt.Split('/');
                    bool first = true;
                    switch (condition[0])
                    {
                        case "level":
                            try
                            {
                                Level_min = int.Parse(condition[1]);
                                Level_max = int.Parse(condition[2]);
                            }
                            catch
                            {
                                Level_min = 0;
                                Level_max = 50;
                            }
                            break;

                        case "guild":
                            GuildNames = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    GuildNames.Add(name);
                            }
                            break;

                        case "guildA":
                            GuildNamesA = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first) first = false;
                                else GuildNamesA.Add(name);
                            }
                            break;

                        case "race":
                            Races = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    Races.Add(name);
                            }
                            break;

                        case "class":
                            Classes = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    Classes.Add(name);
                            }
                            break;

                        case "heure":
                            try
                            {
                                Heure_min = int.Parse(condition[1]);
                                Heure_max = int.Parse(condition[2]);
                            }
                            catch
                            {
                                Heure_min = 0;
                                Heure_max = 24;
                            }
                            break;

						case "reput":
							try
							{
								Reput_min = float.Parse(condition[1]);
								Reput_max = float.Parse(condition[2]);
							}
							catch
							{
								Reput_min = -5;
								Reput_max = 20;
							}
							break;

                        case "quest":
                            CanGiveQuest = true;
                            break;
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Retourne la condition pour être sauvegardé dans la db
        /// </summary>
        public string GetConditionString()
        {
            StringBuilder txt = new StringBuilder();

            if (Level_min != 0 || Level_max != 50)
                txt.Append("level/" + Level_min + "/" + Level_max + "\n");

            if (GuildNames.Count >= 1)
            {
                txt.Append("guild");
                foreach (string guild in GuildNames)
                    txt.Append("/" + guild);
                txt.Append("\n");
            }

            if (GuildNamesA.Count >= 1)
            {
                txt.Append("guildA");
                foreach (string guild in GuildNamesA)
                    txt.Append("/" + guild);
                txt.Append("\n");
            }

            if (Races.Count >= 1)
            {
                txt.Append("race");
                foreach (string race in Races)
                    txt.Append("/" + race);
                txt.Append("\n");
            }

            if (Classes.Count >= 1)
            {
                txt.Append("class");
                foreach (string classe in Classes)
                    txt.Append("/" + classe);
                txt.Append("\n");
            }

        	if (Heure_min > 0 || Heure_max < 24)
        		txt.Append("hour/").Append(Heure_min).Append('/').Append(Heure_max).Append('\n');
			if (Reput_min > BlacklistMgr.MinReputation || Reput_max < BlacklistMgr.MaxReputation)
				txt.Append("reput/").Append(Reput_min).Append('/').Append(Reput_max).Append('\n');

        	if (CanGiveQuest)
                txt.Append("quest\n");

            return txt.ToString();
        }

        public bool CheckAccess(GamePlayer player)
        {
            //level
            if (Level_min > player.Level || player.Level > Level_max)
                return false;
            //Karma
            //			if(!KarmaScriptMgr.Instance().CheckTextNPCAcces(player, Karma_min, Karma_max))
            //				return false;

            //Guilde
            if (GuildNames.Contains(player.GuildName) || (player.GuildName == "" && GuildNames.Contains("NO GUILD")))
                return false;
            if (!GuildNamesA.Contains("ALL") && (!GuildNamesA.Contains(player.GuildName) ||
                (player.GuildName == "" && !GuildNamesA.Contains("NO GUILD"))))
                return false;
            //Classe
            if (Classes.Contains(((eCharacterClass)player.CharacterClass.ID).ToString().ToLower()))
                return false;
            //Race
            if (Races.Contains(player.RaceName.ToLower()))
                return false;

            //Heure
            int heure = (int)(WorldMgr.GetCurrentGameTime() / 1000 / 60 / 54);
            if (Heure_max < Heure_min && (Heure_min > heure || heure <= Heure_max))
                return false;
            if (Heure_max > Heure_min && (Heure_min > heure || heure >= Heure_max))
                return false;
            if (Heure_max == Heure_min && heure != Heure_min)
                return false;
			if (player is AmtePlayer)
			{
				var p = (AmtePlayer) player;
				if (p.Blacklist.Reputation < Reput_min || p.Blacklist.Reputation > Reput_max)
					return false;
			}
            return true;
        }
    }
}
