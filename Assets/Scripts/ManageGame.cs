using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;

public class ManageGame : MonoBehaviour
{
    public static ManageGame instance;

    public string attackedCountry;
    public string playerTribe = "Atlanteans";
    public string difficulty = "hard";  // "easy", "medium"
    public Country allocateTroopsCountry;

    public bool battleHasEnded;
    public bool battleWon;
    public bool gameEnded;
    public bool playerTurn;
    public bool turnOver;
    public bool playerTroopAllocate;
    public bool troopAllocateOver;
    public bool manouvreOver;

    public int exp;
    public int money;
    public int numPlayers;
    public int turn = 0;
    public int iturn = 0;
    public int playerIndex = 0;

    public string[] players = System.Enum.GetNames(typeof(Country.ControllingPlayers));

    public GameObject mtPanel;

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

        // get player index
        for (int i = 0; i < numPlayers; i++)
        {
            if (players[i] == playerTribe)
            {
                playerIndex = i;
            }
        }

        instance.playerTurn = false;
        instance.playerTroopAllocate = false;

        mtPanel = GameObject.Find("ManouvrePanel");
        mtPanel.SetActive(false);

        // begin main game loop
        StartCoroutine(gameLoop());
    }

    // the main game loop
    private IEnumerator gameLoop()
    {
        while (true)
        {
            for (int i = 0; i < players.Length; i++)
            {
                // if (iturn > 0) { i = iturn; continue; }  // skip to saved turn

                // update money and exp UI
                GameObject.Find("expText").GetComponent<Text>().text = instance.exp.ToString();
                GameObject.Find("moneyText").GetComponent<Text>().text = "$" + instance.money.ToString();

                Text promptText = GameObject.Find("PromptText").GetComponent<Text>();
                tintTextColor("", promptText);

                removePlayers();    // remove players that have no countries left

                Dictionary<string, List<string>> playerCountries = getPlayerCountriesDict();

                if (!playerCountries.ContainsKey(players[playerIndex]))   // If the player runs out of countries, game over
                {
                    print("You Lose");
                    break;
                }

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

                if (i == playerIndex)
                    yield return playerMove(reinforcements, currPlayerCountries);
                else if (players[i] != "")
                {
                    aiMove(i, reinforcements, playerCountries);
                    yield return new WaitForSeconds(2.0f);
                }
                iturn++;
            }
        }
    }

    public void highlightPlayerCountries(List<string> countryList)
    {
        GameObject[] countries = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject country in countries)
        {
            CountryHandler countHandler = country.GetComponent<CountryHandler>();
            if (countryList.Contains(countHandler.country.name.ToString()))
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
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(255, 251, 0, opacity)); }
                if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 94, 13, opacity)); }
            }
            else
                countHandler.TintColor(new Color32(1, 1, 1, 150));
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

    // AI Logic
    private void aiMove(int i, int reinforcements, Dictionary<string, List<string>> playerCountries)
    {
        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = players[i];
        tintTextColor(players[i], ptText);

        // Pick random country to allocate reinforcements
        List<string> ownedCountries = playerCountries[players[i]];
        int randomInt = Random.Range(0, ownedCountries.Count);
        string randomCountry = ownedCountries[randomInt];
        GameObject.Find(randomCountry).GetComponent<CountryHandler>().country.troops += reinforcements;
        refreshTroopsLabels();

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

        print(players[i] + ": " + attacking_country + " vs " + targetCountry);

        // RNG for whether attacker wins 0 -> 1
        int num = Random.Range(0, 2);
        Text promptText = GameObject.Find("PromptText").GetComponent<Text>();
        
        if (num == 1)
        {
            promptText.text = players[i] + " captured " + targetCountry;
            tintTextColor(players[i], promptText);
            CountryHandler count = GameObject.Find(targetCountry).GetComponent<CountryHandler>();
            count.country.controllingPlayer = (Country.ControllingPlayers)System.Enum.Parse(typeof(Country.ControllingPlayers), players[i]);
            ownedCountries = playerCountries[players[i]];
            highlightPlayerCountries(ownedCountries);

            // if attacker wins, defending country troops = (attacking country troops -1) and attacking country troops = 1
            print(attacking_country);
            GameObject.Find(targetCountry).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops - 1;
            GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = 1;
        }
        else
        {
            promptText.text = "";

            // if attacker loses, attacker's troops are halved
            print(attacking_country);
            GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops / 2;
        }
    }

    private IEnumerator playerMove(int reinforcements, List<string> countries)
    {
        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        ptText.text = players[playerIndex] + " (YOU)";
        tintTextColor(players[playerIndex], ptText);
        print(iturn + ": YOU");
        instance.turnOver = false;
        instance.troopAllocateOver = false;
        instance.manouvreOver = false;

        // Assign reinforcements
        instance.playerTroopAllocate = true;
        CountryManager.instance.TintCountries();    // tint other countries grey
        GameObject.Find("PromptText").GetComponent<Text>().text = "Choose a country to allocate " + reinforcements.ToString() + " reinforcements";
        while (!instance.troopAllocateOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.allocateTroopsCountry.troops += reinforcements;
        refreshTroopsLabels();
        instance.playerTroopAllocate = false;

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

        // Attack
        GameObject.Find("PromptText").GetComponent<Text>().text = "Choose a country to attack";

        instance.playerTurn = true;
        while (!instance.turnOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.playerTurn = false;
    }

    public void moveTroops()
    {
        Dropdown numTroopsDD = GameObject.Find("NumTroops").GetComponent<Dropdown>();
        Dropdown countryA = GameObject.Find("CountryA").GetComponent<Dropdown>();
        Dropdown countryB = GameObject.Find("CountryB").GetComponent<Dropdown>();

        int numTroops = int.Parse(numTroopsDD.options[numTroopsDD.value].text);
        string fromCountry = countryA.options[countryA.value].text;
        string toCountry = countryB.options[countryB.value].text;

        GameObject.Find(fromCountry).GetComponent<CountryHandler>().country.troops -= numTroops;
        GameObject.Find(toCountry).GetComponent<CountryHandler>().country.troops += numTroops;

        numTroopsDD.RefreshShownValue();

        refreshTroopsLabels();
    }

    // refresh country troops labels
    public void refreshTroopsLabels()
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

    // remove players with no countries left
    public void removePlayers()
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
}
