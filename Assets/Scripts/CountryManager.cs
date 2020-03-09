using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // switch country ownership to player when they win the battle and set money/exp rewards
        if (ManageGame.instance.battleHasEnded && ManageGame.instance.battleWon)
        {
            CountryHandler count = GameObject.Find(ManageGame.instance.attackedCountry).GetComponent<CountryHandler>();
            count.country.controllingPlayer = Country.ControllingPlayers.Atlanteans;
            ManageGame.instance.exp += count.country.expReward;
            ManageGame.instance.money += count.country.moneyReward;
            TintCountries();
        }
        ManageGame.instance.Saving(); // save game
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
        byte opacitity = 225;
        for (int i = 0; i < countryList.Count; i++)
        {
            CountryHandler countHandler = countryList[i].GetComponent<CountryHandler>();
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Aborigines) { countHandler.TintColor(new Color32(153, 116, 61, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Annunaki) { countHandler.TintColor(new Color32(199, 113, 227, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Atlanteans) { countHandler.TintColor(new Color32(95, 175, 237, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Babylonians) { countHandler.TintColor(new Color32(95, 237, 185, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Celts) { countHandler.TintColor(new Color32(206, 242, 133, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Clovis) { countHandler.TintColor(new Color32(255, 0, 0, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Iberians) { countHandler.TintColor(new Color32(0, 1, 64, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Maya) { countHandler.TintColor(new Color32(255, 135, 0, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Nommo) { countHandler.TintColor(new Color32(105, 29, 62, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Olmecs) { countHandler.TintColor(new Color32(255, 251, 0, opacitity)); }
            if (countHandler.country.controllingPlayer == Country.ControllingPlayers.Zoroastrians) { countHandler.TintColor(new Color32(0, 94, 13, opacitity)); }
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
        SceneManager.LoadScene("Fight");
        ManageGame.instance.turnOver = true;
        ManageGame.instance.Saving(); // save game
    }
}
