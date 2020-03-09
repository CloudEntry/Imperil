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

    public bool battleHasEnded;
    public bool battleWon;

    public bool gameEnded;

    public bool playerTurn = true;
    public bool turnOver = false;

    public int exp;
    public int money;
    public int turn = 0;

    public string playerTribe = "Atlanteans";

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

        // begin main game loop
        gameLoop();
    }

    // the main game loop
    public async Task gameLoop()
    {
        string[] players = System.Enum.GetNames(typeof(Country.ControllingPlayers));
        Text ptText = GameObject.Find("PlayerTurnText").GetComponent<Text>();

        while (true)
        {
            ptText.text = players[instance.turn];
            if (players[instance.turn] == playerTribe)
            {
                instance.turn += 1;
                ptText.text = ptText.text + " (YOU)";
                instance.playerTurn = true;
                while (turnOver == false)
                {
                    await Task.Delay(10);
                }
            }
            else
            {
                instance.playerTurn = false;
                print(players[instance.turn] + " is doing AI stuff");
                await Task.Delay(1000);
            }
            if (instance.turn == 10)
            {
                instance.turn -= 11;
            }
            instance.turn += 1;
            Saving();
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
        print("loading");
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
