using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Country
{
    public string name;

    public enum ControllingPlayers
    {
        Aborigines,
        Annunaki,
        Atlanteans,
        Babylonians,
        Celts,
        Clovis,
        Iberians,
        Maya,
        Nommo,
        Olmecs,
        Zoroastrians
    }

    public ControllingPlayers controllingPlayer;

    public int moneyReward;
    public int expReward;

    public int troops;
}
