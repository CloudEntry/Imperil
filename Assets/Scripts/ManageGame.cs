using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ManageGame : MonoBehaviour
{
    public static ManageGame instance;

    public string attackedCountry;
    public string difficulty = "hard";  // "easy", "medium"
    public string playerTribe;
    public List<string> playerTribes = new List<string>();

    public Country allocateTroopsCountry;

    public bool battleHasEnded;
    public bool battleWon;
    public bool gameEnded;
    public bool playerTurn;
    public bool turnOver;
    public bool playerTroopAllocate;
    public bool troopAllocateOver;
    public bool manouvreOver;
    public bool opponentTurn;
    public bool opponentTurnOver;
    public bool troops100Flag;
    public bool troops500Flag;
    public bool troops1000Flag;

    public int exp;
    public int money;
    public int level;
    public int numPlayers;
    public int turn = 0;
    public int iturn = 0;

    public string[] players = System.Enum.GetNames(typeof(Country.ControllingPlayers));

    public GameObject mtPanel;
    public GameObject igPanel;
    public GameObject stPanel;

    public Client client;

    public string cmov;

    [System.Serializable]
    public class SaveData
    {
        public List<Country> savedCountries = new List<Country>();
        public int cur_money;
        public int cur_exp;
        public int cur_turn;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        numPlayers = players.Length;

        client = FindObjectOfType<Client>();
        playerTribe = client.tribe;

        foreach (GameClient p in client.players)
        {
            if (p.name != client.clientName)
                playerTribes.Add(p.tribe);
        }

        instance.playerTurn = false;
        instance.playerTroopAllocate = false;

        mtPanel = GameObject.Find("ManouvrePanel");
        mtPanel.SetActive(false);
        igPanel = GameObject.Find("IGMenuPanel");
        igPanel.SetActive(false);
        stPanel = GameObject.Find("StorePanel");
        stPanel.SetActive(false);

        // begin main game loop
        StartCoroutine(gameLoop());
    }

    private IEnumerator gameLoop()  // the main game loop
    {
        while (true)
        {
            for (int i = 0; i < players.Length; i++)
            {
                // if (iturn > 0) { i = iturn; continue; }  // skip to saved turn

                // update level, money and exp UI
                int[] lvlExp = CheckLevel();
                level = lvlExp[0];
                GameObject.Find("lvlText").GetComponent<Text>().text = level.ToString();
                if (lvlExp[1] == 0)
                    GameObject.Find("expText").GetComponent<Text>().text = "(" + instance.exp.ToString() + ")";
                else
                    GameObject.Find("expText").GetComponent<Text>().text = "(" + instance.exp.ToString() + " / " + lvlExp[1] + ")";
                GameObject.Find("moneyText").GetComponent<Text>().text = "$" + instance.money.ToString();

                Text promptText = GameObject.Find("PromptText").GetComponent<Text>();
                tintTextColor("", promptText);

                removePlayers();    // remove players that have no countries left

                Dictionary<string, List<string>> playerCountries = getPlayerCountriesDict();

                if (!playerCountries.ContainsKey(client.tribe))   // If the player runs out of countries, game over
                    SceneManager.LoadScene("Menu");
                    // yield return GameOver();

                List<string> currPlayerCountries = new List<string>();

                if (playerCountries.ContainsKey(players[i]))
                    currPlayerCountries = playerCountries[players[i]];
                else continue;

                // show player countries in UI Panel
                string text = "";
                foreach (string country in currPlayerCountries)
                    text += country + ", ";
                text = text.Remove(text.Length - 2, 2);
                Text ptText = GameObject.Find("PlayerCountriesText").GetComponent<Text>();
                ptText.text = text;
                tintTextColor(players[i], ptText);

                refreshTroopsLabels();
                highlightPlayerCountries(currPlayerCountries);  // highlight current player countries

                // get reinforcements based on how many countries player owns
                int reinforcements = currPlayerCountries.Count;

                if (players[i] == playerTribe)
                {
                    yield return playerMove(reinforcements, currPlayerCountries);
                }
                else if (playerTribes.Contains(players[i]))
                {
                    yield return waitForOpponentMove(i);
                }
                else if (players[i] != "")
                {
                    if (client.isHost)  // only process AI moves on host
                    {
                        aiMove(i, reinforcements, playerCountries);
                        yield return new WaitForSeconds(2.0f);
                    }
                    else
                    {
                        yield return waitForAIMove(i);
                    }
                }

                // highlightPlayerCountries(currPlayerCountries);  // highlight countries after attack :- later change with some animation

                iturn++;
            }
        }
    }

    public int[] CheckLevel()
    {
        int lvl = 1;
        int expNeeded = 20;

        if (exp >= 20 && exp < 50)
        {
            lvl = 2;
            expNeeded = 50;
        }
        if (exp >= 50 && exp < 90)
        {
            lvl = 3;
            expNeeded = 90;
        }
        if (exp >= 90 && exp < 130)
        {
            lvl = 4;
            expNeeded = 130;
        }
        if (exp >= 130 && exp < 170)
        {
            lvl = 5;
            expNeeded = 170;
        }
        if (exp >= 170 && exp < 220)
        {
            lvl = 6;
            expNeeded = 220;
        }
        if (exp >= 220 && exp < 270)
        {
            lvl = 7;
            expNeeded = 270;
        }
        if (exp >= 270 && exp < 320)
        {
            lvl = 8;
            expNeeded = 320;
        }
        if (exp >= 320 && exp < 370)
        {
            lvl = 9;
            expNeeded = 370;
        }
        if (exp >= 370)
        {
            lvl = 10;
            expNeeded = 0;
        }

        int[] lvlExp = { lvl, expNeeded };
        return lvlExp;
    }   // get the player's level and exp needed to reach next level

    public IEnumerator GameOver()
    {
        print("You Lose");

        while (true)
            yield return null;
    }

    public IEnumerator waitForOpponentMove(int i)
    {
        // CountryManager.instance.TintCountries();

        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = players[i];
        tintTextColor(players[i], ptText);

        string turnTribe = "";
        foreach (GameClient p in client.players)
        {
            if (p.tribe == players[i])
            {
                turnTribe = p.name;
                break;
            }
        }
        GameObject.Find("PromptText").GetComponent<Text>().text = turnTribe + "'s turn"; 

        print("Waiting for opponent move");
        instance.opponentTurn = true;
        instance.opponentTurnOver = false;
        while (!instance.opponentTurnOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.opponentTurn = false;
        instance.opponentTurnOver = false;
    }

    public IEnumerator waitForAIMove(int i)
    {
        // CountryManager.instance.TintCountries();

        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = players[i];
        tintTextColor(players[i], ptText);
        Text prText = GameObject.Find("PromptText").GetComponent<Text>();
        prText.text = players[i] + "'s turn";
        tintTextColor(players[i], prText);

        print("Waiting for AI move");
        instance.opponentTurn = true;
        instance.opponentTurnOver = false;
        while (!instance.opponentTurnOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.opponentTurn = false;
        instance.opponentTurnOver = false;
    }

    public void processOpponentMove(string data)
    {
        string[] dataArr = data.Split('|');

        string opp_str = dataArr[1];
        string reinforcements_cmd = dataArr[2];
        string manouvre_cmd = "";
        string attack_cmd = "";

        string[] opp_arr = opp_str.Split(':');
        string opp_type = opp_arr[0];
        string opponent = opp_arr[1];

        if (opponent == client.clientName)
            return;

        if (dataArr.Length == 5)
        {
            manouvre_cmd = dataArr[3];
            attack_cmd = dataArr[4];
        }
        else
            attack_cmd = dataArr[3];

        // print("============================");

        // Reinforcements
        // print(opponent + " reinforcements: " + reinforcements_cmd);
        string[] rc_str = reinforcements_cmd.Split(':');
        string r_country = rc_str[0];
        int r_troops = System.Convert.ToInt32(rc_str[1]);
        GameObject.Find(r_country).GetComponent<CountryHandler>().country.troops += r_troops;

        // Manouvre
        // print(opponent + " manouvre: " + manouvre_cmd);
        if (manouvre_cmd != "" && opp_type == "player")
        {
            string[] mc_str = manouvre_cmd.Split(',');
            string[] mc_arr = new string[3];
            string mc_fromCountry;
            string mc_toCountry;
            int mc_troops;
            foreach (string mc in mc_str)
            {
                mc_arr = mc.Split(':');
                mc_fromCountry = mc_arr[0];
                mc_troops = System.Convert.ToInt32(mc_arr[1]);
                mc_toCountry = mc_arr[2];
                GameObject.Find(mc_fromCountry).GetComponent<CountryHandler>().country.troops -= mc_troops;
                GameObject.Find(mc_toCountry).GetComponent<CountryHandler>().country.troops += mc_troops;
            }
        }
        else if (opp_type == "ai")
        {
            // print("----------------------------------------");
            // print(manouvre_cmd);
            string[] mc_str = manouvre_cmd.Split(',');
            string[] mc_arr = new string[2];
            string mc_country;
            int mc_troops;
            foreach (string mc in mc_str)
            {
                mc_arr = mc.Split(':');
                mc_country = mc_arr[0];
                // print("================> " + mc_arr[1]);
                mc_troops = System.Convert.ToInt32(mc_arr[1]);
                GameObject.Find(mc_country).GetComponent<CountryHandler>().country.troops = mc_troops;
            }
            // print("----------------------------------------");
        }

        // Attack
        // print(opponent + " attack: " + attack_cmd);
        string[] ac_str = attack_cmd.Split(':');
        string ac_def_country = ac_str[0];
        string ac_win = ac_str[1];
        string ac_att_country = ac_str[2];
        Text promptText = GameObject.Find("PromptText").GetComponent<Text>();

        string tribe = "";
        if (opp_type == "player")
        {
            for (int i = 0; i < client.players.Count; i++)  // get opponent index
            {
                if (client.players[i].name == opponent)
                {
                    tribe = client.players[i].tribe;
                    break;
                }
            }
        }
        else
        {
            tribe = opponent;
        }

        if (ac_win == "WON")
        {
            promptText.text = tribe + " captured " + ac_def_country;
            tintTextColor(tribe, promptText);
            CountryHandler count = GameObject.Find(ac_def_country).GetComponent<CountryHandler>();
            count.country.controllingPlayer = (Country.ControllingPlayers)System.Enum.Parse(typeof(Country.ControllingPlayers), tribe);
            // if attacker wins, defending country troops = (attacking country troops -1) and attacking country troops = 1
            GameObject.Find(ac_def_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(ac_att_country).GetComponent<CountryHandler>().country.troops - 1;
            GameObject.Find(ac_att_country).GetComponent<CountryHandler>().country.troops = 1;
        }
        else
        {
            promptText.text = "";
            // if attacker loses, attacker's troops are halved
            GameObject.Find(ac_att_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(ac_att_country).GetComponent<CountryHandler>().country.troops / 2;
        }

        // print("============================");
        instance.opponentTurnOver = true;

        //CountryManager.instance.TintCountries();
    }

    public void highlightPlayerCountries(List<string> countryList)
    {
        //GameObject[] countries = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        //foreach (GameObject country in countries)
        //{
        //    CountryHandler countHandler = country.GetComponent<CountryHandler>();
        //    if (countryList.Contains(countHandler.country.name.ToString()))
        //    {
        //        byte opacity = 255;
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Aborigines) { countHandler.TintColor(new Color32(153, 116, 61, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Annunaki) { countHandler.TintColor(new Color32(199, 113, 227, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Atlanteans) { countHandler.TintColor(new Color32(95, 175, 237, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Babylonians) { countHandler.TintColor(new Color32(95, 237, 185, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Celts) { countHandler.TintColor(new Color32(206, 242, 133, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Clovis) { countHandler.TintColor(new Color32(255, 0, 0, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Iberians) { countHandler.TintColor(new Color32(0, 1, 200, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Maya) { countHandler.TintColor(new Color32(255, 135, 0, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Nommo) { countHandler.TintColor(new Color32(105, 29, 62, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(190, 190, 0, opacity)); }
        //        if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 94, 13, opacity)); }
        //    }
        //    else
        //        countHandler.TintColor(new Color32(1, 1, 1, 150));
        //}

        // untidy way to do it due to unity bug
        string[] worldCountries = {"Atlantis", "Iceweld", "Alba", "Hyperborea", "Jotunland", "Jotunskaard", "Europa", "Serbek", "Italia",
                                   "Hesperia", "Canaria", "West Eria", "Central Eria", "East Eria", "Mehica", "Guyan", "Tamoachan", "Brasilia",
                                   "Patagonia", "Argentia", "Jambu", "Orenia", "India", "Orientos", "Nippon", "Malay", "West Lemuria",
                                   "Central Lemuria","East Lemuria", "West Mu", "East Mu", "Sierra", "Gondwana", "Congo", "Soma", "Lechan",
                                   "Punt", "Levant", "Sumeria", "Babylonia"};

        foreach (string wc in worldCountries)
        {
            CountryHandler countHandler = GameObject.Find(wc).GetComponent<CountryHandler>();
            if (countryList.Contains(wc))
            {
                byte opacity = 255;
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Aborigines) { countHandler.TintColor(new Color32(153, 116, 61, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Annunaki) { countHandler.TintColor(new Color32(199, 113, 227, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Atlanteans) { countHandler.TintColor(new Color32(95, 175, 237, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Babylonians) { countHandler.TintColor(new Color32(95, 237, 185, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Celts) { countHandler.TintColor(new Color32(206, 242, 133, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Clovis) { countHandler.TintColor(new Color32(255, 0, 0, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Iberians) { countHandler.TintColor(new Color32(0, 1, 200, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Maya) { countHandler.TintColor(new Color32(255, 135, 0, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Nommo) { countHandler.TintColor(new Color32(105, 29, 62, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(190, 190, 0, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 94, 13, opacity)); }
            }
            else
            {
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Aborigines) { countHandler.TintColor(new Color32(82, 61, 30, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Annunaki) { countHandler.TintColor(new Color32(105, 61, 120, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Atlanteans) { countHandler.TintColor(new Color32(55, 102, 138, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Babylonians) { countHandler.TintColor(new Color32(48, 120, 94, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Celts) { countHandler.TintColor(new Color32(104, 125, 62, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Clovis) { countHandler.TintColor(new Color32(82, 2, 2, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Iberians) { countHandler.TintColor(new Color32(0, 0, 77, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Maya) { countHandler.TintColor(new Color32(128, 68, 0, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Nommo) { countHandler.TintColor(new Color32(59, 16, 34, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(89, 89, 0, 255)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 48, 7, 255)); }
            }
        }
    }

    public void tintTextColor(string player, Text ptText)
    {
        if (player.Length == 0) { ptText.color = new Color32(255, 255, 255, 255); }
        if (player == "Aborigines") { ptText.color = new Color32(153, 116, 61, 255); }
        if (player == "Annunaki") { ptText.color = new Color32(199, 113, 227, 255); }
        if (player == "Atlanteans") { ptText.color = new Color32(95, 175, 237, 255); }
        if (player == "Babylonians") { ptText.color = new Color32(95, 237, 185, 255); }
        if (player == "Celts") { ptText.color = new Color32(206, 242, 133, 255); }
        if (player == "Clovis") { ptText.color = new Color32(255, 0, 0, 255); }
        if (player == "Iberians") { ptText.color = new Color32(0, 1, 200, 255); }
        if (player == "Maya") { ptText.color = new Color32(255, 135, 0, 255); }
        if (player == "Nommo") { ptText.color = new Color32(105, 29, 62, 255); }
        if (player == "Olmecs") { ptText.color = new Color32(255, 251, 0, 255); }
        if (player == "Zoroastrians") { ptText.color = new Color32(0, 94, 13, 255); }
    }

    public Dictionary<string, List<string>> getPlayerCountriesDict()
    {
        Dictionary<string, List<string>> playerCountries = new Dictionary<string, List<string>>();
        GameObject[] theArray = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject theCountry in theArray)
        {
            CountryHandler countHandler = theCountry.GetComponent<CountryHandler>();
            if (playerCountries.ContainsKey(countHandler.country.controllingPlayer.ToString()))
            {
                playerCountries[countHandler.country.controllingPlayer.ToString()].Add(countHandler.country.name.ToString());
            }
            else
            {
                playerCountries[countHandler.country.controllingPlayer.ToString()] = new List<string>() { countHandler.country.name.ToString() };
            }
        }

        // test
        string text = "";
        foreach (KeyValuePair<string, List<string>> kvp in playerCountries)
        {
            string tribeText = kvp.Key + ": ";
            foreach (string country in kvp.Value)
                tribeText += country + ", ";
            text += tribeText + " \n";
        }
        // print(text);

        return playerCountries;
    }
 
    private void aiMove(int i, int reinforcements, Dictionary<string, List<string>> playerCountries)    // AI Logic
    {
        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = players[i];
        tintTextColor(players[i], ptText);

        cmov = "CMOV|ai:" + players[i] + "|";

        // Pick random country to allocate reinforcements
        List<string> ownedCountries = playerCountries[players[i]];
        int randomInt = Random.Range(0, ownedCountries.Count);
        string randomCountry = ownedCountries[randomInt];

        // Apply reinforcements bonus
        CountryHandler cth = GameObject.Find(randomCountry).GetComponent<CountryHandler>();
        if (GameObject.Find(cth.cityCountry["Cadiz"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            reinforcements *= 3;
        if (GameObject.Find(cth.cityCountry["Camelot"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            reinforcements *= 2;
        if (GameObject.Find(cth.cityCountry["Giza"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            reinforcements *= 5;
        if (GameObject.Find(cth.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            reinforcements *= 2;
        if (GameObject.Find(cth.cityCountry["Dropa"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            reinforcements *= 3;

        GameObject.Find(randomCountry).GetComponent<CountryHandler>().country.troops += reinforcements;
        refreshTroopsLabels();

        cmov += randomCountry + ":" + reinforcements.ToString() + "|";

        // Don't manouvre troops on easy
        if (instance.difficulty == "medium") // manouvre troops to maintain even distribution
        {
            int total_troops = 0;
            foreach (string oc in ownedCountries)
                total_troops += GameObject.Find(oc).GetComponent<CountryHandler>().country.troops;
            int ave_troops = total_troops / ownedCountries.Count;
            foreach (string oc in ownedCountries)
            {
                GameObject.Find(oc).GetComponent<CountryHandler>().country.troops = ave_troops;
            }
            int left_over_troops = total_troops - (ave_troops * ownedCountries.Count);
            for (int n = 0; n < ownedCountries.Count; n++)
            {
                if (n == left_over_troops)
                    break;
                GameObject.Find(ownedCountries[n]).GetComponent<CountryHandler>().country.troops += 1;
            }
        }
        else if (instance.difficulty == "hard") // more troops to countries bordering more enemy tribes
        {
            int nc_count;
            int tot_nb_count = 0;
            int total_troops = 0;
            List<int> nc_counts = new List<int>();
            foreach (string oc in ownedCountries)
            {
                nc_count = 0;
                string[] nbCountries = GameObject.Find(oc).GetComponent<CountryHandler>().neighbourCountries[oc];
                foreach (string nb in nbCountries)
                {
                    string nb_player = GameObject.Find(nb).GetComponent<CountryHandler>().country.controllingPlayer.ToString();
                    if (nb_player != players[i])
                    {
                        nc_count += 1;
                        tot_nb_count += 1;
                    }
                }
                nc_counts.Add(nc_count);
                total_troops += GameObject.Find(oc).GetComponent<CountryHandler>().country.troops;
            }
            int total_ts = 0;
            for (int j = 0; j < ownedCountries.Count; j++)
            {
                int troops = GameObject.Find(ownedCountries[j]).GetComponent<CountryHandler>().country.troops;
                int troopShare = System.Convert.ToInt32(((double)nc_counts[j] / tot_nb_count) * total_troops);
                total_ts += troopShare;
                GameObject.Find(ownedCountries[j]).GetComponent<CountryHandler>().country.troops = troopShare;
            }
            string mt_country = ownedCountries[0];
            int troop_diff = total_troops - total_ts;
            foreach (string oc in ownedCountries)
            {
                if (GameObject.Find(oc).GetComponent<CountryHandler>().country.troops > GameObject.Find(mt_country).GetComponent<CountryHandler>().country.troops)
                    mt_country = oc;
            }
            if (total_ts != total_troops)   // if over/underallocated troops due to rounding - remove/add troop to country with most troops
                GameObject.Find(mt_country).GetComponent<CountryHandler>().country.troops += troop_diff;
            
            // allocate 1 troop from country with most troops to each country with 0
            int zero_troop_countries = 0;
            foreach (string oc in ownedCountries)
            {
                if (GameObject.Find(oc).GetComponent<CountryHandler>().country.troops == 0)
                {
                    zero_troop_countries += 1;
                    GameObject.Find(oc).GetComponent<CountryHandler>().country.troops = 1;
                }
            }
            GameObject.Find(mt_country).GetComponent<CountryHandler>().country.troops -= zero_troop_countries;
        }

        // manouvre troops cmd
        foreach (string oc in ownedCountries)
        {
            cmov += oc + ":" + GameObject.Find(oc).GetComponent<CountryHandler>().country.troops + ",";
        }

        // Attack
        List<string> tcCandidates = new List<string>();
        // Pick target country
        foreach (string oc in ownedCountries)
        {
            string[] targetCountries = GameObject.Find(oc).GetComponent<CountryHandler>().neighbourCountries[oc];
            foreach (string tc in targetCountries)
                if (GameObject.Find(tc).GetComponent<CountryHandler>().country.controllingPlayer.ToString() != players[i] && !tcCandidates.Contains(GameObject.Find(tc).GetComponent<CountryHandler>().country.name.ToString()))
                    tcCandidates.Add(GameObject.Find(tc).GetComponent<CountryHandler>().country.name.ToString());
        }

        string targetCountry = "";

        if (instance.difficulty == "easy")  // easy just attacks first country
        {
            targetCountry = tcCandidates[0];
            // get the attacking country
        }
        else if (instance.difficulty == "medium")  // medium attacks weakest country
        {
            int troops;
            int w_troops = GameObject.Find(tcCandidates[0]).GetComponent<CountryHandler>().country.troops;
            string w_name = tcCandidates[0];
            foreach (string tcc in tcCandidates)
            {
                troops = GameObject.Find(tcc).GetComponent<CountryHandler>().country.troops;
                if (troops < w_troops)
                {
                    w_troops = troops;
                    w_name = tcc;
                }
            }
            targetCountry = w_name;
            // get the attacking country
        }
        else if (instance.difficulty == "hard")  // hard attacks country with best attacking/defending troops ratio
        {
            targetCountry = tcCandidates[0];
            double ratio;
            double best_ratio = -1000000.0;
            string best_ratio_country = "";
            foreach (string oc in ownedCountries)
            {
                string[] targetCountries = GameObject.Find(oc).GetComponent<CountryHandler>().neighbourCountries[oc];
                foreach (string tc in targetCountries)
                {
                    if (GameObject.Find(tc).GetComponent<CountryHandler>().country.controllingPlayer.ToString() != players[i])
                        {
                        ratio = (double)GameObject.Find(oc).GetComponent<CountryHandler>().country.troops / GameObject.Find(tc).GetComponent<CountryHandler>().country.troops;
                        if (ratio > best_ratio)
                        {
                            best_ratio = ratio;
                            best_ratio_country = tc;
                        }
                    }
                }
            }
            targetCountry = best_ratio_country;
        }

        // get attacking country
        int att_most_troops = 0;
        string att_mt_country = "";
        string[] att_nbCountries = GameObject.Find(targetCountry).GetComponent<CountryHandler>().neighbourCountries[targetCountry];
        foreach (string nc in att_nbCountries)
        {
            if (GameObject.Find(nc).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            {
                int att_troops = GameObject.Find(nc).GetComponent<CountryHandler>().country.troops;
                if (att_troops > att_most_troops)
                {
                    att_most_troops = att_troops;
                    att_mt_country = nc;
                }
            }
        }
        string attacking_country = att_mt_country;

        int attTroops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops;
        int defTroops = GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.troops;

        // adjust for city perks
        CountryHandler atc = GameObject.Find(attacking_country).GetComponent<CountryHandler>();
        if (GameObject.Find(atc.cityCountry["Chichen Itza"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.1);
        if (GameObject.Find(atc.cityCountry["Akakor"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.4);
        if (GameObject.Find(atc.cityCountry["Antilla"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.25);
        if (GameObject.Find(atc.cityCountry["Cadiz"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.1);
        if (GameObject.Find(atc.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.1);
        if (GameObject.Find(atc.cityCountry["Madjedbebe"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == players[i])
            attTroops += (int)(attTroops * 0.5);

        CountryHandler dfc = GameObject.Find(targetCountry).GetComponent<CountryHandler>();
        if (GameObject.Find(dfc.cityCountry["Chichen Itza"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.4);
        if (GameObject.Find(dfc.cityCountry["Akakor"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.1);
        if (GameObject.Find(dfc.cityCountry["Antilla"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.25);
        if (GameObject.Find(dfc.cityCountry["Camelot"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.25);
        if (GameObject.Find(dfc.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.1);
        if (GameObject.Find(dfc.cityCountry["Gobekli Tepe"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.5);
        if (GameObject.Find(dfc.cityCountry["Dropa"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.controllingPlayer)
            defTroops += (int)(defTroops * 0.1);

        // print(attacking_country + " (" + atc.country.controllingPlayer + ") " + atc.country.troops + "->" + attTroops + " vs " + targetCountry + " (" + dfc.country.controllingPlayer + ") " + dfc.country.troops + "->" + defTroops);

        double troop_odds = (double)attTroops / defTroops;
        double trunc_troop_odds = System.Math.Truncate(troop_odds * 100) / 100; 
        int rng = (int)(trunc_troop_odds * 10);
        bool battle_won = false;

        if (troop_odds > 1) // biased on how many attacking vs defending troops
        {
            if (Random.Range(0, rng) != 1)
                battle_won = true;
        }
        else
        {
            if (Random.Range(0, rng) == 1)
                battle_won = true;
        }
        Text promptText = GameObject.Find("PromptText").GetComponent<Text>();
        string result = "";

        if (battle_won)
        {
            result = "WON";
            promptText.text = players[i] + " captured " + targetCountry;
            tintTextColor(players[i], promptText);
            CountryHandler count = GameObject.Find(targetCountry).GetComponent<CountryHandler>();
            count.country.controllingPlayer = (Country.ControllingPlayers)System.Enum.Parse(typeof(Country.ControllingPlayers), players[i]);
            ownedCountries = playerCountries[players[i]];
            highlightPlayerCountries(ownedCountries);

            // if attacker wins, defending country troops = (attacking country troops -1) and attacking country troops = 1
            GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops - 1;
            GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = 1;
        }
        else
        {
            result = "LOST";
            promptText.text = "";

            // if attacker loses, attacker's troops are halved
            GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops / 2;
        }
        // print(result);

        cmov = cmov.Remove(cmov.Length - 1, 1); // remove last comma
        cmov += "|" + targetCountry + ":" + result + ":" + attacking_country;
        // print(cmov);
        client.Send(cmov);
    }

    private IEnumerator playerMove(int reinforcements, List<string> countries)
    {
        //CountryManager.instance.TintCountries();

        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = client.tribe + " (YOU)";
        tintTextColor(client.tribe, ptText);
        // print(iturn + ": YOU");
        instance.turnOver = false;
        instance.troopAllocateOver = false;
        instance.manouvreOver = false;

        cmov = "CMOV|player:" + client.clientName + "|";

        // Apply reinforcements bonus
        CountryHandler cth = GameObject.Find(countries[0]).GetComponent<CountryHandler>();
        if (GameObject.Find(cth.cityCountry["Cadiz"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == client.tribe)
            reinforcements *= 3;
        if (GameObject.Find(cth.cityCountry["Camelot"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == client.tribe)
            reinforcements *= 2;
        if (GameObject.Find(cth.cityCountry["Giza"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == client.tribe)
            reinforcements *= 5;
        if (GameObject.Find(cth.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == client.tribe)
            reinforcements *= 2;
        if (GameObject.Find(cth.cityCountry["Dropa"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == client.tribe)
            reinforcements *= 3;

        // Add store upgrades and reset flags
        if (troops100Flag)
            reinforcements += 100;
        if (troops500Flag)
            reinforcements += 500;
        if (troops1000Flag)
            reinforcements += 1000;
        troops100Flag = false;
        troops500Flag = false;
        troops1000Flag = false;

        // Assign reinforcements
        instance.playerTroopAllocate = true;
        // CountryManager.instance.TintCountries();    // tint other countries grey
        GameObject.Find("PromptText").GetComponent<Text>().text = "Choose a country to allocate " + reinforcements.ToString() + " reinforcements";
        while (!instance.troopAllocateOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.allocateTroopsCountry.troops += reinforcements;
        refreshTroopsLabels();
        instance.playerTroopAllocate = false;

        cmov += instance.allocateTroopsCountry.name + ":" + reinforcements.ToString() + "|";

        // Manouvre troops
        mtPanel.SetActive(true);
        Dropdown numTroopsDD = GameObject.Find("NumTroops").GetComponent<Dropdown>();
        Dropdown countryA = GameObject.Find("CountryA").GetComponent<Dropdown>();
        Dropdown countryB = GameObject.Find("CountryB").GetComponent<Dropdown>();
        countryA.options.Clear();
        countryB.options.Clear();
        numTroopsDD.options.Clear();
        foreach (string country in countries)
        {
            countryA.options.Add(new Dropdown.OptionData() { text = country });
            countryB.options.Add(new Dropdown.OptionData() { text = country });
        }
        int old_valueA = countryA.value;

        int numTroops = GameObject.Find(countries[countryA.value]).GetComponent<CountryHandler>().country.troops - 1;
        List<string> numTroopList = new List<string>();
        for (int i = numTroops; i > 0; i--)
            numTroopList.Add(i.ToString());
        foreach (string num in numTroopList)
            numTroopsDD.options.Add(new Dropdown.OptionData() { text = num });

        while (!instance.manouvreOver)
        {
            if (countryA.value != old_valueA)
            {
                old_valueA = countryA.value;

                numTroopsDD.options.Clear();
                numTroopList.Clear();
                numTroops = GameObject.Find(countries[countryA.value]).GetComponent<CountryHandler>().country.troops - 1;
                for (int i = numTroops; i > 0; i--)
                    numTroopList.Add(i.ToString());
                foreach (string num in numTroopList)
                {
                    numTroopsDD.options.Add(new Dropdown.OptionData() { text = num });
                    numTroopsDD.RefreshShownValue();
                }
            }
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        }
        CountryManager.instance.TintCountries();    // tint countries back to normal

        cmov = cmov.Remove(cmov.Length - 1, 1); // remove last comma

        // Attack
        GameObject.Find("PromptText").GetComponent<Text>().text = "Choose a country to attack";

        instance.playerTurn = true;
        while (!instance.turnOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.playerTurn = false;

        //cmov += "|" + attackedCountry + ":" + battleWon;
        //print(cmov);
        //client.Send(cmov);
    }

    public void moveTroops()
    {
        Dropdown numTroopsDD = GameObject.Find("NumTroops").GetComponent<Dropdown>();
        Dropdown countryA = GameObject.Find("CountryA").GetComponent<Dropdown>();
        Dropdown countryB = GameObject.Find("CountryB").GetComponent<Dropdown>();

        int numTroops = int.Parse(numTroopsDD.options[numTroopsDD.value].text);
        string fromCountry = countryA.options[countryA.value].text;
        string toCountry = countryB.options[countryB.value].text;

        if (GameObject.Find(fromCountry).GetComponent<CountryHandler>().country.troops < 2)   // don't manoeuvre troops if < 2
        {
            // print("Not enough troops to manoeuvre");
        }
        else
        {
            GameObject.Find(fromCountry).GetComponent<CountryHandler>().country.troops -= numTroops;
            GameObject.Find(toCountry).GetComponent<CountryHandler>().country.troops += numTroops;
        }

        numTroopsDD.RefreshShownValue();
        refreshTroopsLabels();

        cmov += fromCountry + ":" + numTroops.ToString() + ":" + toCountry + ",";
    }

    public void refreshTroopsLabels()   // refresh country troops labels
    {
        GameObject[] theArray = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject theCountry in theArray)
        {
            CountryHandler countHandler = theCountry.GetComponent<CountryHandler>();
            string name = countHandler.country.name.ToString();
            string lname = Regex.Replace(name, @"\s+", "") + "Text";
            GameObject.Find(lname).GetComponent<Text>().text = name.Substring(0, 3) + "." + countHandler.country.troops.ToString();
        }
    }

    public void removePlayers() // remove players with no countries left
    {
        List<string> activePlayers = new List<string>();
        GameObject[] theArray = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject theCountry in theArray)
        {
            CountryHandler countHandler = theCountry.GetComponent<CountryHandler>();
            activePlayers.Add(countHandler.country.controllingPlayer.ToString());
        }
        for (int i = 0; i < instance.players.Length; i++)
        {
            if (string.IsNullOrEmpty(instance.players[i])) continue;
            if (!activePlayers.Contains(instance.players[i]))
            {
                instance.players[i] = "";
            }
        }
    }

    public void Saving()
    {
        SaveData data = new SaveData();
        for (int i = 0; i < CountryManager.instance.countryList.Count; i++)
        {
            // save all data from countries in the SavaData class
            data.savedCountries.Add(CountryManager.instance.countryList[i].GetComponent<CountryHandler>().country);
        }
        // money, exp & turn
        data.cur_exp = instance.exp;
        data.cur_money = instance.money;
        data.cur_turn = instance.turn;

        BinaryFormatter bf = new BinaryFormatter();
        FileStream stream = new FileStream(Application.persistentDataPath + "/SaveFile.dat", FileMode.Create);

        bf.Serialize(stream, data);
        stream.Close();
    }

    public void Loading()
    {
        if (File.Exists(Application.persistentDataPath + "/SaveFile.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(Application.persistentDataPath + "/SaveFile.dat", FileMode.Open);

            SaveData data = (SaveData)bf.Deserialize(stream);
            stream.Close();

            for (int i = 0; i < data.savedCountries.Count; i++)
            {
                for (int j = 0; j < CountryManager.instance.countryList.Count; j++)
                {
                    if (data.savedCountries[i].name == CountryManager.instance.countryList[j].GetComponent<CountryHandler>().country.name)
                    {
                        CountryManager.instance.countryList[j].GetComponent<CountryHandler>().country = data.savedCountries[i];
                    }
                }
            }
            instance.exp = data.cur_exp;
            instance.money = data.cur_money;
            instance.turn = data.cur_turn;

            // CountryManager.instance.TintCountries();
        }
        else
        {
            // print("No Saved File Found");
        }
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(Application.persistentDataPath + "/SaveFile.dat"))
        {
            File.Delete(Application.persistentDataPath + "/SaveFile.dat");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // In-Game Menu Button functions
    public void ShowMenu()
    {
        igPanel.SetActive(true);
    }
    public void ChangeDiffButton()
    {
        string[] diffArr = { "hard", "medium", "easy" };
        Dropdown AIDiffDropdown = GameObject.Find("AIDiffDropdown").GetComponent<Dropdown>();
        difficulty = diffArr[AIDiffDropdown.value];
    }
    public void QuitButton()
    {
        SceneManager.LoadScene("Menu");
    }
    public void BackButton()
    {
        igPanel.SetActive(false);
    }

    // Store menu function
    public void OpenStore()
    {
        stPanel.SetActive(true);
        // disable buttons if perk in use or level too low
        if (troops100Flag) 
            GameObject.Find("Troops100Button").GetComponent<Button>().interactable = false;
        else
            GameObject.Find("Troops100Button").GetComponent<Button>().interactable = true;

        if (troops500Flag || level < 5)
            GameObject.Find("Troops500Button").GetComponent<Button>().interactable = false;
        else
            GameObject.Find("Troops500Button").GetComponent<Button>().interactable = true;

        if (troops1000Flag || level < 10)
            GameObject.Find("Troops1000Button").GetComponent<Button>().interactable = false;
        else
            GameObject.Find("Troops1000Button").GetComponent<Button>().interactable = true;
    }
    public void CloseStore()
    {
        stPanel.SetActive(false);
    }
    public void Set100TroopsFlag()
    {
        if (money >= 3000)
        {
            troops100Flag = true;
            GameObject.Find("Troops100Button").GetComponent<Button>().interactable = false;
            money -= 3000;
        }
    }
    public void Set500TroopsFlag()
    {
        if (money >= 7000)
        {
            troops500Flag = true;
            GameObject.Find("Troops500Button").GetComponent<Button>().interactable = false;
            money -= 7000;
        }
    }
    public void Set1000TroopsFlag()
    {
        if (money >= 10000)
        {
            troops1000Flag = true;
            GameObject.Find("Troops1000Button").GetComponent<Button>().interactable = false;
            money -= 10000;
        }
    }
}
