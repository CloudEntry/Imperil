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

    public bool battleHasEnded;
    public bool battleWon;
    public bool gameEnded;
    public bool playerTurn = true;
    public bool turnOver = false;

    public int exp;
    public int money;
    public int numPlayers;
    public int turn = 0;
    public int iturn = 0;
    public int playerIndex = 0;

    public string[] players = System.Enum.GetNames(typeof(Country.ControllingPlayers));

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

        // begin main game loop
        StartCoroutine(gameLoop());
    }

    // the main game loop
    private IEnumerator gameLoop()
    {
        while (true)
        {
            // loop through AI players until player's turn
            for (int i = 0; i < playerIndex; i++)
            {
                aiTurn(i);
                yield return new WaitForSeconds(1.0f);
            }
            
            // get player input
            GameObject.Find("PlayerTurnText").GetComponent<Text>().text = players[playerIndex] + " (YOU)";
            iturn++;
            print(iturn + " YOU");
            yield return waitForPlayerTurn();
            
            // loop through rest of AI players
            for (int i = playerIndex + 1; i < players.Length; i++)
            {
                aiTurn(i);
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

    // AI Logic
    private void aiTurn(int i)
    {
        removePlayers();
        if (players[i] != "")
        {
            GameObject.Find("PlayerTurnText").GetComponent<Text>().text = players[i];
            iturn++;
            print(iturn + ": " + players[i] + " doing AI stuff");
        }
    }

    private IEnumerator waitForPlayerTurn()
    {
        bool done = false;
        while (!done) // essentially a "while true", but with a bool to break out naturally
        {
            instance.playerTurn = true;
            if (turnOver)
            {
                turnOver = false;
                instance.playerTurn = false;
                done = true; // breaks the loop
            }
            yield return null; // wait until next frame, then continue execution from here (loop continues)
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
