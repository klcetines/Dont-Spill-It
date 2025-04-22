using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RoomManager : MonoBehaviour
{
    private const int MAX_PLAYERS_PER_ROOM = 6;

    public static RoomManager Instance;
    public string currentRoomCode;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private GameObject imagePositions;
    [SerializeField] private GameObject playerImagePrefab;

    private WebSocketManager WSManager;

    // Almacena los clientes y sus representaciones visuales
    private Dictionary<string, PlayerInfo> players = new Dictionary<string, PlayerInfo>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            WSManager = FindFirstObjectByType<WebSocketManager>();
            if(WSManager == null){
                Debug.LogError("WebSocketManager not found from RoomManager");
            }
            GetRoomCode(WSManager);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public class PlayerInfo
    {
        public WebSocketClient client;
        public GameObject visualRepresentation;
    }

    public void GetRoomCode(WebSocketManager WSManager)
    {
        string currentRoomCode = WSManager.RoomCode;
        roomCodeText.text = currentRoomCode;
        Debug.Log($"Código de sala generado: {currentRoomCode}");
    }

    public bool RegisterPlayer(string playerName, WebSocketClient client)
    {
        if (players.Count >= MAX_PLAYERS_PER_ROOM)
        {
            Debug.LogWarning("La sala está llena");
            return false;
        }

        if (players.ContainsKey(playerName))
        {
            Debug.LogWarning($"El jugador {playerName} ya existe");
            return false;
        }

        // Crear entrada para el nuevo jugador
        players.Add(playerName, new PlayerInfo {
            client = client,
            visualRepresentation = CreatePlayerVisual(playerName)
        });

        Debug.Log($"Jugador {playerName} registrado en la sala");
        return true;
    }

    private GameObject CreatePlayerVisual(string playerName)
    {
        if (players.Count < imagePositions.transform.childCount)
        {
            Transform targetPos = imagePositions.transform.GetChild(players.Count);
            GameObject playerImage = Instantiate(playerImagePrefab, targetPos.position, Quaternion.identity, imagePositions.transform);
            
            TextMeshProUGUI nameText = playerImage.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null) nameText.text = playerName;
            
            return playerImage;
        }
        return null;
    }

    public void RemovePlayer(string playerName)
    {
        if (players.TryGetValue(playerName, out PlayerInfo playerInfo))
        {
            if (playerInfo.visualRepresentation != null)
                Destroy(playerInfo.visualRepresentation);
            
            if (playerInfo.client != null)
                Destroy(playerInfo.client.gameObject);
            
            players.Remove(playerName);
            Debug.Log($"Jugador {playerName} eliminado de la sala");
        }
    }

    public List<string> GetPlayerNames()
    {
        return new List<string>(players.Keys);
    }

    public WebSocketClient GetPlayerClient(string playerName)
    {
        return players.TryGetValue(playerName, out PlayerInfo info) ? info.client : null;
    }

    public void BroadcastToAll(string message)
    {
        foreach (var player in players.Values)
        {
            SendToClient(player.client.PlayerName, message);
        }
    }

    public void SendToClient(string playerName, string message) 
    {
        WebSocketClient client = GetPlayerClient(playerName);
        if (client != null)
        {
            client.Send($"{playerName}|{message}");
        }    
    }

    public List<string> GetRoomClients()
    {
        return new List<string>(players.Keys);
    }
}