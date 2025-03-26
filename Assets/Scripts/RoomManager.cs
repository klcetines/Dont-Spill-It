using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RoomManager : MonoBehaviour
{
    private const int MAX_PLAYERS_PER_ROOM = 6; 

    public static RoomManager Instance;
    public Dictionary<string, List<WebSocketClient>> activeRooms = new Dictionary<string, List<WebSocketClient>>();
    public string currentRoomCode;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private GameObject imagePositions;
    [SerializeField] private GameObject playerImagePrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateRoomCode();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GenerateRoomCode()
    {
        currentRoomCode = Random.Range(1000, 9999).ToString(); // Código de 4 dígitos
        Debug.Log($"Código de sala generado: {currentRoomCode}");
        roomCodeText.text = currentRoomCode;
        activeRooms.Add(currentRoomCode, new List<WebSocketClient>());
    }

    public bool JoinRoom(string code, WebSocketClient client)
    {
        if (activeRooms.ContainsKey(code) && activeRooms[code].Count < MAX_PLAYERS_PER_ROOM)
        {
            activeRooms[code].Add(client);
            int peopleJoined = activeRooms[currentRoomCode].Count;
            if ((peopleJoined - 1) < imagePositions.transform.childCount)
            {
                Transform targetPosition = imagePositions.transform.GetChild(peopleJoined - 1);
                InstantiatePlayerImage(targetPosition.position);
            }
            else
            {
                Debug.LogWarning("Not enough child positions available for the player image.");
            }

            return true;
        }
        return false;
    }

    public void InstantiatePlayerImage(Vector3 position)
    {
        if (playerImagePrefab != null && imagePositions != null)
        {
            Instantiate(playerImagePrefab, position, Quaternion.identity, imagePositions.transform);
            Debug.Log("Player image instantiated at position: " + position);
        }
        else
        {
            Debug.LogWarning("Player image prefab or image positions parent is not assigned.");
        }
    }
}