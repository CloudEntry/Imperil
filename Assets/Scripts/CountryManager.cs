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
        ManageGame.instance.Loading(); // load game and tint countries accordingly
        TintCountries();
    }

    // Tints the country depending on which player is controlling it
    public void TintCountries()
    {
        byte opacity = 150;
        for (int i = 0; i < countryList.Count; i++)
        {
            CountryHandler countHandler = countryList[i].GetComponent<CountryHandler>();
            if (ManageGame.instance.playerTroopAllocate) {
                if (countHandler.country.controllingPlayer.ToString() != ManageGame.instance.playerTribe) { countHandler.TintColor(new Color32(1, 1, 1, opacity)); }
            }
            else
            {
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
        GameObject.Find("PromptText").GetComponent<Text>().text = "";

        //SceneManager.LoadScene("Fight");

        // RNG for whether attacker wins 0 -> 1
        int num = Random.Range(0, 2);
        if (num == 1)
        {
            CountryHandler count = GameObject.Find(ManageGame.instance.attackedCountry).GetComponent<CountryHandler>();
            count.country.controllingPlayer = Country.ControllingPlayers.Atlanteans;
            ManageGame.instance.exp += count.country.expReward;
            ManageGame.instance.money += count.country.moneyReward;
            TintCountries();
        }
        DisableAttackPanel();
        ManageGame.instance.turnOver = true;
        ManageGame.instance.Saving(); // save game
    }
}
