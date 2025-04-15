using UnityEngine;
using WebSocketSharp;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket _clientSocket;
    public string PlayerName { get; private set; }
    
    public void Initialize(string playerName, string roomCode)
    {
        DontDestroyOnLoad(this);
        PlayerName = playerName;
        _clientSocket = new WebSocket($"ws://192.168.0.25:8080/client/{roomCode}/{playerName}");
        
        _clientSocket.OnMessage += (sender, e) => {
            Debug.Log($"[{PlayerName}] Mensaje recibido: {e.Data}");
            MainThreadDispatcher.ExecuteOnMainThread(() => {
                ProcessMessage(e.Data);
            });
            
        };
        
        _clientSocket.Connect();
    }

    public void Send(string message)
    {
        if (_clientSocket != null && _clientSocket.IsAlive)
        {
            _clientSocket.Send(message);
        }
    }

    private void ProcessMessage(string message)
    {
        if(GameManager.Instance != null){
            // Procesar mensajes entrantes para este cliente específico
            GameManager.Instance.HandlePlayerAction(PlayerName, message);
        }
    }

    void OnDestroy()
    {
        if (_clientSocket != null)
        {
            if (_clientSocket.IsAlive) _clientSocket.Close();
            _clientSocket = null;
        }
    }
}