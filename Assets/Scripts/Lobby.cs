using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Lobby : MonoBehaviour
{
    public static Lobby Instance { set; get; }
    public Client client;
    public List<string> tribes = new List<string>() { "Aborigines", "Annunaki", "Atlanteans", "Babylonians", "Celts", "Clovis", "Iberians", "Maya", "Nommo", "Olmecs", "Zoroastrians" };
    public int playersReady = 0;

    private void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Awake()
    {
        client = FindObjectOfType<Client>();
        GameObject.Find("PlayerText").GetComponent<Text>().text = client.clientName;

        // Add new clients to UI;
        int x = 1;
        for (int i = 0; i < client.players.Count; i++)
        {
            if (client.players[i].name != client.clientName)
            {
                GameObject.Find("Player" + x.ToString() + "Text").GetComponent<Text>().text = client.players[i].name;
                x += 1;
            }
        }
        StartCoroutine(WaitToStartGame());
    }

    private IEnumerator WaitToStartGame()
    {
        // Wait for all players to be ready then start game
        while (true)
        {
            if (playersReady == client.numPlayers)
            {
                SceneManager.LoadScene("Game");
                break;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void Ready()
    {
        // Set tribe in dropdown as player tribe;
        int index = GameObject.Find("TribeDropdown").GetComponent<Dropdown>().value;
        client.tribe = tribes[index];
        client.Send("CTRB|" + client.clientName + "|" + tribes[index]);
        GameObject.Find("ReadyButton").GetComponent<Button>().interactable = false;
        GameObject.Find("TribeDropdown").GetComponent<Dropdown>().interactable = false;
        playersReady += 1;
    }

    public void UpdatePlayerTribes(string data)
    {
        string[] arr = data.Split('|');
        string player = arr[1];
        string tribe = arr[2];

        // Show client tribe
        for (int i = 1; i < 4; i++)
        {
            if (player == GameObject.Find("Player" + i.ToString() + "Text").GetComponent<Text>().text)
            {
                GameObject.Find("Tribe" + i.ToString() + "Text").GetComponent<Text>().text = tribe;
            }
        }

        // Update dropdown
        Dropdown TribeDropdown = GameObject.Find("TribeDropdown").GetComponent<Dropdown>();
        for (int x = 0; x < TribeDropdown.options.Count; x++)
        {
            if (TribeDropdown.options[x].text == tribe)
            {
                TribeDropdown.options.RemoveAt(x);
                tribes.RemoveAt(x);
                break;
            }
        }
        if (player != client.clientName)
            playersReady += 1;
    }
}
