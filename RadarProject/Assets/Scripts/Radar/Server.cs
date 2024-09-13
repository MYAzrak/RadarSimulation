using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class Server : MonoBehaviour
{
    public static Server serverInstance;
    public bool start = false;

    public WebSocketServer server;

    // Start is called before the first frame update
    void Awake()
    {
        serverInstance = this;

        if (server == null)
            server = new WebSocketServer("ws://localhost:8080");
    }

    void Start()
    {
        //server.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (start)
        {
            StartServer();
            start = false;
        }
    }

    void StartServer()
    {
        server.Start();
    }

    void OnApplicationQuit()
    {
        server.Stop();
        Debug.Log("Stopped Server");
    }
}

public class DataService : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        // Handle incoming messages if needed
        Debug.Log($"Received message: {e.Data}");
    }

    protected override void OnOpen()
    {
        Debug.Log("Client connected");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("Client disconnected");
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error: {e.Message}");
    }
}
