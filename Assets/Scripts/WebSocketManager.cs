using WebSocketSharp;
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
        RoomCode = Random.Range(1000, 9999).ToString();
        _mainSocket = new WebSocket($"ws://192.168.0.25:8080/host/{RoomCode}");
        
        _mainSocket.OnMessage += (sender, e) => {
            Debug.Log($"Mensaje recibido en Unity: {e.Data}");
            
            var data = e.Data.Split('|');
            
            // Manejar nuevo jugador
            if (data[0] == "NEW_PLAYER")
            {
                string playerName = data[1];
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    CreateClientConnection(playerName);
                });
            }
            // Manejar mensajes de jugadores
            else if (data.Length > 1)
            {
                string playerName = data[0];
                string message = data[1];
                
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    ProcessClientMessage(playerName, message);
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

    private void ProcessClientMessage(string playerName, string message)
    {
        Debug.Log($"Procesando mensaje de {playerName}: {message}");
        
        if (_connectedClients.TryGetValue(playerName, out WebSocketClient client))
        {
            // Ejemplo: manejar acción "THROW"
            if (message == "THROW")
            {
                GameManager.Instance.HandlePlayerAction(playerName, "throw");
            }
        }
    }

    public void SendToClient(string playerName, string message)
    {
        if (_connectedClients.TryGetValue(playerName, out WebSocketClient client))
        {
            client.Send(message);
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