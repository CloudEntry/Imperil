using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.SceneManagement;

public class CountryManager : MonoBehaviour
{
    public static CountryManager instance;

    public GameObject attackPanel;

    public List<GameObject> countryList = new List<GameObject>();

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        attackPanel.SetActive(false);
        AddCountryData();
    }

    // Inbuilt Unity function gets all the game objects with 'country' tag and adds them to list
    void AddCountryData()
    {
        GameObject[] theArray = GameObject.FindGameObjectsWithTag("Country") as GameObject[];
        foreach (GameObject country in theArray)
        {
            countryList.Add(country);
        }
        // ManageGame.instance.Loading(); // load game and tint countries accordingly
        // TintCountries();
        ManageGame.instance.refreshTroopsLabels();
    }

    // Tints the country depending on which player is controlling it
    public void TintCountries()
    {
        byte opacity = 255;
        for (int i = 0; i < countryList.Count; i++)
        {
            CountryHandler countHandler = countryList[i].GetComponent<CountryHandler>();
            if (ManageGame.instance.playerTroopAllocate) {
                if (countHandler.country.controllingPlayer.ToString() != ManageGame.instance.playerTribe) { countHandler.TintColor(new Color32(1, 1, 1, 150)); }
            }
            else
            {
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
        }
    }

    // Method to show attack panel and display description, money and exp rewards in labels
    public void ShowAttackPanel(string description, int moneyReward, int expReward)
    {
        attackPanel.SetActive(true);
        AttackPanel gui = attackPanel.GetComponent<AttackPanel>();
        gui.descriptionText.text = description;
        gui.moneyRewardText.text = "+" + moneyReward.ToString();
        gui.expRewardText.text = "+" + expReward.ToString();
    }

    // Method to hide attack panel
    public void DisableAttackPanel()
    {
        attackPanel.SetActive(false);
    }

    public void StartBattle()
    {
        ManageGame.instance.battleWon = false;

        Text promptText = GameObject.Find("PromptText2").GetComponent<Text>();
        promptText.text = "";

        //SceneManager.LoadScene("Fight");

        string attacked_country = ManageGame.instance.attackedCountry;
        // get attacking country = country with the most troops
        int most_troops = 0;
        string mt_country = "";
        string[] nbCountries = GameObject.Find(attacked_country).GetComponent<CountryHandler>().neighbourCountries[attacked_country];
        foreach (string nc in nbCountries)
        {
            if (GameObject.Find(nc).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
            {
                int troops = GameObject.Find(nc).GetComponent<CountryHandler>().country.troops;
                if (troops > most_troops)
                {
                    most_troops = troops;
                    mt_country = nc;
                }
            }
        }
        string attacking_country = mt_country;

        int attTroops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops;
        int defTroops = GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.troops;

        if (attTroops < 2)  // can't attack with troops < 2
        {
            DisableAttackPanel();
            promptText.text = "Not Enough Troops";
        }
        else
        {
            // adjust for city perks
            CountryHandler atc = GameObject.Find(attacking_country).GetComponent<CountryHandler>();
            if (GameObject.Find(atc.cityCountry["Chichen Itza"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.1);
            if (GameObject.Find(atc.cityCountry["Akakor"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.4);
            if (GameObject.Find(atc.cityCountry["Antilla"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.25);
            if (GameObject.Find(atc.cityCountry["Cadiz"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.1);
            if (GameObject.Find(atc.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.1);
            if (GameObject.Find(atc.cityCountry["Madjedbebe"]).GetComponent<CountryHandler>().country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                attTroops += (int)(attTroops * 0.5);

            CountryHandler dfc = GameObject.Find(attacked_country).GetComponent<CountryHandler>();
            if (GameObject.Find(dfc.cityCountry["Chichen Itza"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.4);
            if (GameObject.Find(dfc.cityCountry["Akakor"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.1);
            if (GameObject.Find(dfc.cityCountry["Antilla"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.25);
            if (GameObject.Find(dfc.cityCountry["Camelot"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.25);
            if (GameObject.Find(dfc.cityCountry["Babylon"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.1);
            if (GameObject.Find(dfc.cityCountry["Gobekli Tepe"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.5);
            if (GameObject.Find(dfc.cityCountry["Dropa"]).GetComponent<CountryHandler>().country.controllingPlayer == GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.controllingPlayer)
                defTroops += (int)(defTroops * 0.1);

            print(attacking_country + " (" + atc.country.controllingPlayer + ") " + atc.country.troops + "->" + attTroops + " vs " + attacked_country + " (" + dfc.country.controllingPlayer + ") " + dfc.country.troops + "->" + defTroops);


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

            string result = "";

            if (battle_won)
            {
                result = "WON";
                CountryHandler count = GameObject.Find(attacked_country).GetComponent<CountryHandler>();
                count.country.controllingPlayer = (Country.ControllingPlayers)System.Enum.Parse(typeof(Country.ControllingPlayers), ManageGame.instance.playerTribe);
                ManageGame.instance.exp += count.country.expReward;
                ManageGame.instance.money += count.country.moneyReward;
                TintCountries();
                promptText.text = "YOU WON";

                ManageGame.instance.battleWon = true;

                // if attacker wins, defending country troops = (attacking country troops -1) and attacking country troops = 1
                GameObject.Find(attacked_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops - 1;
                GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = 1;
            }
            else
            {
                result = "LOST";
                promptText.text = "YOU LOST";

                // if attacker loses, attacker's troops are halved
                GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops = GameObject.Find(attacking_country).GetComponent<CountryHandler>().country.troops / 2;
            }

            print(result);

            DisableAttackPanel();
            ManageGame.instance.turnOver = true;
            ManageGame.instance.Saving(); // save game
            StartCoroutine(DeletePromptText());
        }
    }

    private IEnumerator DeletePromptText()
    {
        yield return new WaitForSeconds(2.0f);
        Text promptText2 = GameObject.Find("PromptText2").GetComponent<Text>();
        promptText2.text = "";
    }
}
