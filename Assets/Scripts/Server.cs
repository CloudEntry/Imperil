using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port = 6321; // choose an empty port

    private List<ServerClient> clients;
    private List<ServerClient> disconnectList;

    private TcpListener server;
    private bool serverStarted;

    public void Init()
    {
        DontDestroyOnLoad(gameObject);  // don't destroy server when unity changes between scenes
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;
        }
        catch (Exception e)
        {
            print("Socket error: " + e.Message);
        }
    }

    private void Update()   // to run on linux, run this on a RunLoop() on a different thread
    {
        if (!serverStarted)
            return;

        foreach (ServerClient c in clients)
        {
            // is the client still connected?
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }
            else
            {
                NetworkStream s = c.tcp.GetStream();    // accept data from client
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();

                    if (data != null)
                        OnIncomingData(c, data);
                }
            }
        }

        for (int i = 0; i < disconnectList.Count; i++)
        {
            // tell our player that someone has disconnected

            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
        }
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server); // callback to do handshake when client joins server
    }

    private void AcceptTcpClient(IAsyncResult ar)   // add the client to list of clients
    {
        TcpListener listener = (TcpListener)ar.AsyncState;

        string allUsers = "";
        foreach (ServerClient i in clients) // string of all users
        {
            allUsers += i.clientName + '|';
        }

        ServerClient sc = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(sc);

        StartListening();   // carry on listening for more clients

        Broadcast("SWHO|" + allUsers + GameManager.Instance.numPlayers.ToString(), clients[clients.Count - 1]);  // send list of all users to last user
    }

    private bool IsConnected(TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }

    private void Broadcast(string data, List<ServerClient> cl)  // server send
    {
        foreach (ServerClient sc in cl)
        {
            try
            {
                StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e)
            {
                print("Write error:" + e.Message);
            }
        }
    }

    private void Broadcast(string data, ServerClient c)  // overload puts single client into list and calls broadcast method 
    {
        List<ServerClient> sc = new List<ServerClient> { c };
        Broadcast(data, sc);
    }

    private void OnIncomingData(ServerClient c, string data)   // server read - parse incoming data
    {
        print("Server: " + data);

        string[] aData = data.Split('|');

        // parse first parameter
        switch (aData[0])
        {
            case "CWHO":
                c.clientName = aData[1];
                c.isHost = (aData[2] == "0") ? false : true;
                Broadcast("SCNN|" + c.clientName + "|" + GameManager.Instance.numPlayers.ToString(), clients);  // tell all users a new users has just connected
                break;
            case "CMOV":
                Broadcast(data, clients);
                break;
            case "CTRB":
                Broadcast(data, clients);
                break;
        }
    }
}

public class ServerClient
{
    public string clientName;
    public TcpClient tcp;
    public bool isHost;

    public ServerClient(TcpClient tcp)
    {
        this.tcp = tcp;
    }
}