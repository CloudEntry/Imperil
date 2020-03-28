using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]

public class CountryHandler : MonoBehaviour
{
    public Country country;
    private SpriteRenderer sprite;

    private Color32 oldColor;
    private Color32 hoverColor;

    public Dictionary<string, string[]> neighbourCountries = new Dictionary<string, string[]>()
    {
        {"Atlantis", new string[] {"East Eria", "Iceweld", "Canaria", "Guyan"} },
        {"Iceweld", new string[] {"Atlantis", "Alba"} },
        {"Alba", new string[] {"Iceweld", "Hesperia", "Europa", "Hyperborea"} },
        {"Hyperborea", new string[] {"Alba", "Europa", "Jotunland"} },
        {"Jotunland", new string[] {"Hyperborea","Jotunskaard"} },
        {"Jotunskaard", new string[] {"Jotunland"} },
        {"Europa", new string[] {"Alba", "Hyperborea", "Serbek", "Italia", "Hesperia"} },
        {"Serbek", new string[] {"Europa", "Babylonia"} },
        {"Italia", new string[] {"Europa", "Levant"} },
        {"Hesperia", new string[] {"Canaria", "Alba", "Europa"} },
        {"Canaria", new string[] {"Atlantis", "Hesperia"} },
        {"West Eria", new string[] {"Central Eria", "Mehica"} },
        {"Central Eria", new string[] {"West Eria", "East Eria", "Mehica"} },
        {"East Eria", new string[] {"Central Eria", "Atlantis"} },
        {"Mehica", new string[] {"West Eria", "Central Eria", "Tamoachan"} },
        {"Guyan", new string[] {"Tamoachan", "Brasilia", "Atlantis"} },
        {"Tamoachan", new string[] {"Mehica", "Guyan", "Brasilia", "Patagonia"} },
        {"Brasilia", new string[] {"Tamoachan", "Guyan", "Patagonia"} },
        {"Patagonia", new string[] {"Tamoachan", "Brasilia", "Argentia"} },
        {"Argentia", new string[] {"Patagonia"} },
        {"Jambu", new string[] {"Orenia", "Orientos", "Nippon"} },
        {"Orenia", new string[] {"Sumeria", "Jambu", "Orientos", "India"} },
        {"India", new string[] {"Sumeria", "Orenia"} },
        {"Orientos", new string[] {"Orenia", "Jambu", "Malay", "Central Lemuria"} },
        {"Nippon", new string[] {"Jambu"} },
        {"Malay", new string[] {"Orientos", "West Lemuria"} },
        {"West Lemuria", new string[] {"Central Lemuria", "Malay", "West Mu"} },
        {"Central Lemuria", new string[] {"East Lemuria", "West Lemuria", "West Mu", "Orientos"} },
        {"East Lemuria", new string[] {"Central Lemuria", "East Mu"} },
        {"West Mu", new string[] {"West Lemuria", "East Mu"} },
        {"East Mu", new string[] {"East Lemuria", "West Mu"} },
        {"Sierra", new string[] {"Congo", "Levant", "Gondwana"} },
        {"Gondwana", new string[] {"Sierra", "Levant", "Soma", "Lechan", "Congo"} },
        {"Congo", new string[] {"Sierra", "Gondwana"} },
        {"Soma", new string[] {"Gondwana", "Levant", "Sumeria", "Lechan"} },
        {"Lechan", new string[] {"Gondwana", "Soma", "Punt"} },
        {"Punt", new string[] {"Lechan"} },
        {"Levant", new string[] {"Sierra", "Italia", "Babylonia", "Sumeria" , "Soma", "Gondwana"} },
        {"Sumeria", new string[] {"Orenia", "Levant", "Babylonia", "Soma"} },
        {"Babylonia", new string[] {"Serbek", "Levant", "Sumeria", "Orenia", "India"} },
    };

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    void OnMouseEnter()
    {
        oldColor = sprite.color;
        if (!ManageGame.instance.playerTurn || country.controllingPlayer.ToString() == ManageGame.instance.playerTribe) 
        {
            hoverColor = oldColor;
        }
        else if (country.controllingPlayer.ToString() != ManageGame.instance.playerTribe)
        {
            hoverColor = new Color32(oldColor.r, oldColor.g, oldColor.b, 250);
        }
        sprite.color = hoverColor;
    }

    void OnMouseExit()
    {
        sprite.color = oldColor;
    }

    // Method that shows attack panel if country not owned by player
    private void OnMouseUpAsButton()
    {
        // Checks the player owns a neighbouring country to the country clicked on
        bool neighbourPlayer = false;
        foreach (var x in neighbourCountries[country.name])
        {
            CountryHandler nc = GameObject.Find(x).GetComponent<CountryHandler>();
            if (nc.country.controllingPlayer.ToString() == ManageGame.instance.playerTribe)
                neighbourPlayer = true;
        }
        if ((country.controllingPlayer.ToString() != ManageGame.instance.playerTribe) && ManageGame.instance.playerTurn && neighbourPlayer)
        {
            ShowGUI();
        }
        else if (country.controllingPlayer.ToString() == ManageGame.instance.playerTribe && ManageGame.instance.playerTroopAllocate)
        {
            allocateTroops();
        }
    }

    private void allocateTroops()
    {
        ManageGame.instance.allocateTroopsCountry = country;
        ManageGame.instance.troopAllocateOver = true;
    }

    // sets name variable to name of Component in Unity
    void OnDrawGizmos()
    {
        country.name = name;
        this.tag = "Country";
    }

    public void TintColor(Color32 color)
    {
        sprite.color = color;
    }

    void ShowGUI()
    {
        CountryManager.instance.ShowAttackPanel(country.name + " is owned by the " + country.controllingPlayer.ToString() + ". Are you sure you want to attack?", country.moneyReward, country.expReward);
        ManageGame.instance.attackedCountry = country.name;
        ManageGame.instance.battleHasEnded = false;
        ManageGame.instance.battleWon = false;
    }
}
