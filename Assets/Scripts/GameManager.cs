using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private float turnTimeout = 15f;

    private RoomManager _RoomManager;
    private MainThreadDispatcher _MainThreadDispatcher;
    private WebSocketClient _WebSocketClient;
    private MapPath _MapPath;

    private Dictionary<string, GameObject> _playersDictionary = new Dictionary<string, GameObject>();

    private List<string> _playerOrder = new List<string>();
    private int _currentPlayerIndex = 0;
    private bool _waitingForDiceThrow = false;
    private float _turnTimer = 0f;
    private bool _isGameActive = false;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _RoomManager = FindFirstObjectByType<RoomManager>();
        _MainThreadDispatcher = FindFirstObjectByType<MainThreadDispatcher>();
        _WebSocketClient = FindFirstObjectByType<WebSocketClient>();
        _MapPath = FindFirstObjectByType<MapPath>();

        if (_RoomManager == null || _MainThreadDispatcher == null || _WebSocketClient == null || _MapPath == null)
        {
            Debug.LogError("One or more required components are missing.");
            return;
        }

        StartGame();
    }

    public void StartGame()
    {
        if (_isGameActive) return;

        _playerOrder = _RoomManager.GetRoomClients();
        if (_playerOrder.Count == 0)
        {
            Debug.LogError("No players in the room to start the game");
            return;
        }

        foreach (string name in _playerOrder)
        {
            InstantiatePlayerAtStart(name);
        }

        _isGameActive = true;
        StartPlayerTurn();
    }
    
    void Update()
    {
        if (_waitingForDiceThrow)
        {
            _turnTimer += Time.deltaTime;
            if (_turnTimer >= turnTimeout)
            {
                SkipTurn();
            }
        }
    }

    private void InstantiatePlayerAtStart(string playerName){
        PathNode startNode = _MapPath.GetFirstNode();
        if (startNode != null)
        {
            GameObject player = Instantiate(characterPrefab, startNode.transform.position, Quaternion.identity);
            _playersDictionary[playerName] = player; 

            Character character = player.GetComponent<Character>();
            if (character != null)
            {
                character.SetMapPath(_MapPath);
                character.SetPlayerName(playerName);
            }
        }
        else
        {
            Debug.LogError("Start node not found in the map path.");
        }
    }

    private void StartPlayerTurn()
    {
        _waitingForDiceThrow = true;  // Add this line
        _turnTimer = 0f;              // Reset the timer
        
        foreach (var playerName in RoomManager.Instance.GetPlayerNames())
        {
            string message = (playerName == _playerOrder[_currentPlayerIndex]) ? 
                $"your_turn {turnTimeout}" : 
                $"wait_turn {_playerOrder[_currentPlayerIndex]}";
                
            RoomManager.Instance.SendToPlayer(playerName, message);
        }
    }

    public void HandlePlayerAction(string playerName, string action)
    {
        Debug.Log($"Acción recibida de {playerName}: {action}");
        
        if (action == "throw" && IsPlayerTurn(playerName))
        {
            if (_playersDictionary.TryGetValue(playerName, out GameObject player))
            {
                Character character = player.GetComponent<Character>();
                if (character != null)
                {
                    ThrowDice(playerName);
                }
            }
        }
    }

    public void ThrowDice(string playerName)
    {
        _waitingForDiceThrow = false;
        
        if (_playersDictionary.TryGetValue(playerName, out GameObject player))
        {
            Character character = player.GetComponent<Character>();
            if (character != null)
            {
                int diceValue = character.ThrowDice();
                OnDiceThrown(character, diceValue);
            }
        }
    }

    public void OnDiceThrown(Character character, int diceValue)
    {
        if (character != null)
        {
            character.MoveOnMapPath(diceValue, () => {
                EndTurn();
            });
        }
        
    }

    private void EndTurn(){
        // Pasar al siguiente jugador
        _currentPlayerIndex = (_currentPlayerIndex + 1) % _playerOrder.Count;
        StartPlayerTurn();
    }

    private void SkipTurn()
    {
        Debug.Log($"Turn skipped for {_playerOrder[_currentPlayerIndex]} due to timeout");
        _waitingForDiceThrow = false;
        EndTurn();
    }

    public bool IsPlayerTurn(string playerName)
    {
        return _isGameActive && 
            _waitingForDiceThrow && 
            _playerOrder.Count > _currentPlayerIndex && 
            playerName == _playerOrder[_currentPlayerIndex];
    }

    public string GetCurrentPlayer()
    {
        return _playerOrder.Count > 0 ? _playerOrder[_currentPlayerIndex] : null;
    }
}
