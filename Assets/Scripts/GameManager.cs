using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { set; get; }

    public GameObject mainMenu;
    public GameObject serverMenu;
    public GameObject connectMenu;
    public GameObject scrollArea;

    public GameObject serverPrefab;
    public GameObject clientPrefab;

    public InputField nameInput;

    public int numPlayers;

    private void Start()
    {
        Instance = this;
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    public void ConnectButton()
    {
        mainMenu.SetActive(false);
        scrollArea.SetActive(false);
        connectMenu.SetActive(true);
    }

    public void HostButton()
    {
        // try to create server
        Dropdown PlayersDropdown = GameObject.Find("PlayersDropdown").GetComponent<Dropdown>();
        numPlayers = PlayersDropdown.value + 1;

        try
        {
            Server s = Instantiate(serverPrefab).GetComponent<Server>();
            s.Init();

            Client c = Instantiate(clientPrefab).GetComponent<Client>();    // create client to connect to yourself
            c.clientName = nameInput.text;
            c.isHost = true;

            if (c.clientName == "")
                c.clientName = "Host";
            c.ConnectToServer("127.0.0.1", 6321);
        }
        catch (Exception e)
        {
            print(e.Message);
        }

        mainMenu.SetActive(false);
        scrollArea.SetActive(false);
        serverMenu.SetActive(true);
    }

    public void ConnectToServerButton()
    {
        string hostAddress = GameObject.Find("HostInput").GetComponent<InputField>().text;
        if (hostAddress == "")
            hostAddress = "127.0.0.1";

        // try to create client
        try
        {
            Client c = Instantiate(clientPrefab).GetComponent<Client>();
            c.clientName = nameInput.text;
            if (c.clientName == "")
                c.clientName = "Client";
            c.ConnectToServer(hostAddress, 6321);
            connectMenu.SetActive(false);
            scrollArea.SetActive(true);
        }
        catch (Exception e)
        {
            print(e.Message);
        }
    }

    public void BackButton()
    {
        mainMenu.SetActive(true);
        scrollArea.SetActive(true);
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);

        // So you can't create duplicates when you click back button and connect/host again
        Server s = FindObjectOfType<Server>();
        if (s != null)
            Destroy(s.gameObject);

        Client c = FindObjectOfType<Client>();
        if (c != null)
            Destroy(c.gameObject);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void EnterLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}
