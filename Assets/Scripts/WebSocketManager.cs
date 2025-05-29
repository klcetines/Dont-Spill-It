using WebSocketSharp;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }
    
    private WebSocket _mainSocket;
    private Dictionary<string, WebSocketClient> _connectedClients = new Dictionary<string, WebSocketClient>();
    public string RoomCode { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMainConnection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMainConnection()
    {
        _mainSocket = new WebSocket("ws://PosarAquiLaIP:DespresDelsPuntsElPort/host");
          
        _mainSocket.OnMessage += (sender, e) => {
            Debug.Log($"Mensaje recibido en Unity: {e.Data}");
            
            var data = e.Data.Split('|');
            
            if (data[0] == "ROOM_CODE")
            {
                RoomCode = data[1];
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    StartCoroutine(WaitForRoomManagerAndSetCode(RoomCode));
                });
            }
            else if (data[0] == "NEW_PLAYER")
            {
                string playerName = data[1];
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    CreateClientConnection(playerName);
                });
            }
        };
        
        _mainSocket.OnError += (sender, e) => {
            Debug.LogError($"Error en WebSocket: {e.Message}");
        };
        
        _mainSocket.OnClose += (sender, e) => {
            Debug.Log("Conexión WebSocket cerrada");
        };
        
        _mainSocket.Connect();
    }

    private System.Collections.IEnumerator WaitForRoomManagerAndSetCode(string code)
    {
        RoomManager roomManager = null;
        while (roomManager == null)
        {
            roomManager = FindObjectOfType<RoomManager>();
            if (roomManager == null)
                yield return null; // Espera un frame
        }
        roomManager.SetRoomCode(code);
    }

    private void CreateClientConnection(string playerName)
    {
        if (!_connectedClients.ContainsKey(playerName))
        {
            GameObject clientObj = new GameObject($"Client_{playerName}");
            WebSocketClient client = clientObj.AddComponent<WebSocketClient>();
            client.Initialize(playerName, RoomCode);
            
            _connectedClients.Add(playerName, client);
            RoomManager.Instance.RegisterPlayer(playerName, client);
            
            Debug.Log($"Nuevo cliente creado para {playerName}");
        }
    }

    public void SendToClient(string playerName, string message)
    {
        if (_connectedClients.TryGetValue(playerName, out WebSocketClient client))
        {
            string messageId = Guid.NewGuid().ToString("N").Substring(0, 8);
            client.SendWithConfirmation(message, messageId);
        }
    }

    void OnDestroy()
    {
        if (_mainSocket != null && _mainSocket.IsAlive)
        {
            _mainSocket.Close();
        }
    }
}