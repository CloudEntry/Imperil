using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;

public class ManageGame : MonoBehaviour
{
    public static ManageGame instance;

    public string attackedCountry;
    public string playerTribe = "Atlanteans";
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

        // check how many troops assigned
        print("=================================================");

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

                removePlayers();    // remove players that have no countries left

                Dictionary<string, List<string>> playerCountries = getPlayerCountriesDict();

                List<string> currPlayerCountries = new List<string>();

                if (playerCountries.ContainsKey(players[i]))
                    currPlayerCountries = playerCountries[players[i]];
                else continue;

                // show player countries in UI Panel

                highlightPlayerCountries(currPlayerCountries);  // highlight current player countries

                // get reinforcements based on how many countries player owns
                int reinforcements = currPlayerCountries.Count;

                if (i == playerIndex)
                    yield return playerMove(reinforcements, currPlayerCountries);
                else if (players[i] != "")
                {
                    aiMove(i, reinforcements);
                    yield return new WaitForSeconds(1.0f);
                }
                iturn++;
            }
        }
    }

    public void highlightPlayerCountries(List<string> countryList)
    {
        byte opacity;
        GameObject[] countries = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject country in countries)
        {
            CountryHandler countHandler = country.GetComponent<CountryHandler>();
            if (countryList.Contains(countHandler.country.name.ToString()))
                opacity = 255;
            else
                opacity = 150;
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Aborigines) { countHandler.TintColor(new Color32(153, 116, 61, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Annunaki) { countHandler.TintColor(new Color32(199, 113, 227, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Atlanteans) { countHandler.TintColor(new Color32(95, 175, 237, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Babylonians) { countHandler.TintColor(new Color32(95, 237, 185, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Celts) { countHandler.TintColor(new Color32(206, 242, 133, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Clovis) { countHandler.TintColor(new Color32(255, 0, 0, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Iberians) { countHandler.TintColor(new Color32(0, 1, 64, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Maya) { countHandler.TintColor(new Color32(255, 135, 0, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Nommo) { countHandler.TintColor(new Color32(105, 29, 62, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(255, 251, 0, opacity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 94, 13, opacity)); }
        }
    }

    public void tintTextColor(string player)
    {
        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        if (player == "Aborigines") { ptText.color = new Color32(153, 116, 61, 255); }
        if (player == "Annunaki") { ptText.color = new Color32(199, 113, 227, 255); }
        if (player == "Atlanteans") { ptText.color = new Color32(95, 175, 237, 255); }
        if (player == "Babylonians") { ptText.color = new Color32(95, 237, 185, 255); }
        if (player == "Celts") { ptText.color = new Color32(206, 242, 133, 255); }
        if (player == "Clovis") { ptText.color = new Color32(255, 0, 0, 255); }
        if (player == "Iberians") { ptText.color = new Color32(0, 1, 64, 255); }
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
    private void aiMove(int i, int reinforcements)
    {
        GameObject.Find("PlayerTurnText").GetComponent<Text>().text = players[i];
        tintTextColor(players[i]);
        print(iturn + ": " + players[i] + " doing AI stuff");

        // Easy Difficulty
            // Pick random country to allocate reinforcements

            // Don't manouvre troops

            // Attack random country
    }

    private IEnumerator playerMove(int reinforcements, List<string> countries)
    {
        GameObject.Find("PlayerTurnText").GetComponent<Text>().text = players[playerIndex] + " (YOU)";
        tintTextColor(players[playerIndex]);
        print(iturn + " YOU");
        instance.turnOver = false;
        instance.troopAllocateOver = false;
        instance.manouvreOver = false;

        // Assign reinforcements
        instance.playerTroopAllocate = true;
        CountryManager.instance.TintCountries();    // tint other countries grey
        GameObject.Find("PromptText").GetComponent<Text>().text = "Pick a country to allocate " + reinforcements.ToString() + " reinforcements";
        while (!instance.troopAllocateOver)
            yield return null; // wait until next frame, then continue execution from here (loop continues)
        instance.allocateTroopsCountry.troops += reinforcements;
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

        print("Move " + numTroops + " troops from " + fromCountry + " to " + toCountry);
        GameObject.Find(fromCountry).GetComponent<CountryHandler>().country.troops -= numTroops;
        GameObject.Find(toCountry).GetComponent<CountryHandler>().country.troops += numTroops;

        numTroopsDD.RefreshShownValue();
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

            CountryManager.instance.TintCountries();
        }
        else
        {
            print("No Saved File Found");
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
