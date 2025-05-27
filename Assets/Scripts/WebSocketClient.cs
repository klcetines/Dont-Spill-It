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
        _clientSocket = new WebSocket($"ws://192.168.0.19:8080/client-unity/{roomCode}/{playerName}");    
            
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
            _clientSocket.Send($"TO_CLIENT|{message}");
        }
    }

    private void ProcessMessage(string message)
    {
        if (message.StartsWith("CHARACTER_SELECT"))
        {
            RoomManager.Instance.UpdatePlayerVisual(PlayerName, message);
            Send($"CHARACTER_CONFIRM");
        }
        else if(string.IsNullOrEmpty(message) || MiniGamesManager.Instance == null || GameManager.Instance == null) return;
        else if (message.StartsWith("VOTE"))
        {
            MiniGamesManager.Instance.HandleVote(PlayerName, message);
        }
        else if(message.StartsWith("MINIGAME2_ANSWER")){
            MiniGamesManager.Instance.HandleMatchAnswers(PlayerName, message);
        }
        else if(message.StartsWith("MINIGAME2_VOTE")){
            MiniGamesManager.Instance.HandleMinigame2Vote(PlayerName, message);
        }
        else{
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