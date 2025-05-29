using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections;
using System.Collections.Generic;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket _clientSocket;
    public string PlayerName { get; private set; }
    
    private Dictionary<string, Coroutine> _pendingRetries = new Dictionary<string, Coroutine>();

    public event Action<string> OnMessageConfirmed;

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
            string messageId = Guid.NewGuid().ToString("N").Substring(0, 8);
            SendWithConfirmation(message, messageId);
        }
    }

    private void ProcessMessage(string message)
    {
        if (message.StartsWith("CONFIRM|"))
        {
            string messageId = message.Split('|')[1];
            OnMessageConfirmed?.Invoke(messageId);
        }
        else if (message.StartsWith("CHARACTER_SELECT"))
        {
            RoomManager.Instance.UpdatePlayerVisual(PlayerName, message);
            Send($"{PlayerName}|CHARACTER_CONFIRM");
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

    public void SendWithConfirmation(string message, string messageId, int maxRetries = 3, float timeout = 2f)
    {
        if (_pendingRetries.ContainsKey(messageId))
            return;

        Coroutine retryCoroutine = StartCoroutine(SendWithRetry(message, messageId, maxRetries, timeout));
        _pendingRetries[messageId] = retryCoroutine;
    }

    private IEnumerator SendWithRetry(string message, string messageId, int maxRetries, float timeout)
    {
        int retries = 0;
        bool confirmed = false;

        // Función local para marcar como confirmado
        void OnConfirm(string receivedId)
        {
            if (receivedId == messageId)
                confirmed = true;
        }

        // Suscríbete a la confirmación
        OnMessageConfirmed += OnConfirm;

        while (!confirmed && retries < maxRetries)
        {
            _clientSocket.Send($"{message}|ID:{messageId}");
            yield return new WaitForSeconds(timeout);
            retries++;
        }

        OnMessageConfirmed -= OnConfirm;
        _pendingRetries.Remove(messageId);

        if (!confirmed)
            Debug.LogWarning($"No se pudo confirmar el mensaje {messageId} tras varios intentos.");
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