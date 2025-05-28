using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RoomManager : MonoBehaviour
{
    public class PlayerInfo
    {
        public WebSocketClient client;
        public GameObject visualRepresentation;
        public int characterId = 0;
    }

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetRoomCode(string currentRoomCode)
    {
        roomCodeText.text = currentRoomCode;
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

        // Create empty PlayerInfo first
        players.Add(playerName, new PlayerInfo {
            client = client,
            visualRepresentation = null  // Don't create visual yet
        });

        Debug.Log($"Jugador {playerName} registrado en la sala");
        return true;
    }

    private GameObject CreatePlayerVisual(string playerName)
    {
        // First, clean up any existing visuals in the target position
        int targetIndex = players.Count - 1;
        if (targetIndex >= 0 && targetIndex < imagePositions.transform.childCount)
        {
            Transform targetPos = imagePositions.transform.GetChild(targetIndex);
            // Remove any existing objects at this position
            foreach (Transform child in targetPos)
            {
                Destroy(child.gameObject);
            }

            // Create new visual
            GameObject playerImage = Instantiate(playerImagePrefab, targetPos.position, Quaternion.identity, targetPos);
            TextMeshProUGUI nameText = playerImage.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null) nameText.text = playerName;
            
            return playerImage;
        }
        return null;
    }

    public void UpdatePlayerVisual(string playerName, string message)
    {
        var parts = message.Split('|');
        int characterID = int.Parse(parts[2]);
        if (players.TryGetValue(playerName, out PlayerInfo playerInfo))
        {
            playerInfo.characterId = characterID;
            // First destroy the old visual if it exists
            if (playerInfo.visualRepresentation != null)
            {
                Destroy(playerInfo.visualRepresentation);
                playerInfo.visualRepresentation = null;
            }

            // Create new visual representation
            playerInfo.visualRepresentation = CreatePlayerVisual(playerName);

            if (playerInfo.visualRepresentation != null)
            {
                Sprite newSprite = null;
                switch (characterID)
                {
                    case 0:
                        newSprite = Resources.Load<Sprite>("Sprites/PJ/KLCETIN/klcetin_standing");
                        break;
                    case 1:
                        newSprite = Resources.Load<Sprite>("Sprites/PJ/DISCOBOY/discoboy_standing");
                        break;
                    default:
                        Debug.LogWarning($"ID de personaje no reconocido: {characterID}");
                        break;
                }

                Debug.Log($"Sprite cargado: {newSprite}");
                if (newSprite != null)
                {
                    Debug.Log($"Cargando sprite para el jugador {playerName} con ID {characterID}"); 
                    var spriteRenderer = playerInfo.visualRepresentation.GetComponentInChildren<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = newSprite;
                    }
                }

                Debug.Log($"Visual del jugador {playerName} actualizada");
            }
        }
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
    
    public int GetPlayerCharacterId(string playerName)
    {
        if (players.TryGetValue(playerName, out PlayerInfo playerInfo))
        {
            return playerInfo.characterId;
        }
        return 0;
    }
}