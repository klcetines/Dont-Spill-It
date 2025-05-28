using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BoardGameCamera boardGameCamera;

    [Header("Game Settings")]
    [SerializeField] private float turnTimeout = 15f;
    [SerializeField] private PlayerHUDManager hudManager;
    [SerializeField] private GameObject wellDecisionPanel;
    
    [Header("Character Prefabs")]
    [SerializeField] private GameObject klcetinPrefab;
    [SerializeField] private GameObject discoboyPrefab;
    private Dictionary<int, GameObject> characterPrefabs = new Dictionary<int, GameObject>();

    [Header("Game Objects")]
    [SerializeField] private GameObject klcetinHUDPrefab;
    [SerializeField] private GameObject discoboyHUDPrefab;
    private Dictionary<int, GameObject> hudPrefabs = new Dictionary<int, GameObject>();

    private RoomManager _RoomManager;
    private MainThreadDispatcher _MainThreadDispatcher;
    private MapPath _MapPath;

    private Dictionary<string, GameObject> _playersDictionary = new Dictionary<string, GameObject>();

    private List<string> _playerOrder = new List<string>();
    private int _currentPlayerIndex = 0;
    private bool _waitingForDiceThrow = false;
    private float _turnTimer = 0f;
    private bool _isGameActive = false;
    private bool _isMiniGameActive = false;
    private bool _waitingWellDecision = false;

    private int _roundCount = 0;
    private const int MAX_ROUNDS = 10;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCharacterPrefabs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCharacterPrefabs()
    {
        characterPrefabs.Add(0, klcetinPrefab);
        characterPrefabs.Add(1, discoboyPrefab);
        
        hudPrefabs.Add(0, klcetinHUDPrefab);
        hudPrefabs.Add(1, discoboyHUDPrefab);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _RoomManager = FindFirstObjectByType<RoomManager>();
        _MainThreadDispatcher = FindFirstObjectByType<MainThreadDispatcher>();
        _MapPath = FindFirstObjectByType<MapPath>();
        hudManager = FindFirstObjectByType<PlayerHUDManager>();

        if (_RoomManager == null || _MainThreadDispatcher == null || _MapPath == null || hudManager == null)
        {
            Debug.LogError("One or more required components are missing.");
            return;
        }
        // Add delay to ensure everything is initialized
        StartGame();
    }

    public void StartGame()
    {
        if (_isGameActive) return;

        _playerOrder = _RoomManager.GetRoomClients();
        Debug.Log($"Starting game with {_playerOrder.Count} players: {string.Join(", ", _playerOrder)}");

        if (_playerOrder.Count == 0)
        {
            Debug.LogError("No players in the room to start the game");
            return;
        }

        foreach (string name in _playerOrder)
        {
            InstantiatePlayerAtStart(name);
        }

        // Add delay before starting first turn
        Invoke("InitialTurn", 0.5f);
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

    public GameObject GetHUDPrefab(int characterId)
    {
        return hudPrefabs.ContainsKey(characterId) ? hudPrefabs[characterId] : hudPrefabs[0];
    }

    private void InitialTurn()
    {
        _isGameActive = true;
        StartPlayerTurn();
    }

    private void InstantiatePlayerAtStart(string playerName)
    {
        PathNode startNode = _MapPath.GetFirstNode();
        if (startNode != null)
        {
            int characterId = _RoomManager.GetPlayerCharacterId(playerName);
            Debug.Log($"Instantiating player {playerName} with character ID {characterId}");

            GameObject prefabToUse = characterPrefabs.ContainsKey(characterId) ? 
                characterPrefabs[characterId] : characterPrefabs[0];

            GameObject player = Instantiate(prefabToUse, startNode.transform.position, Quaternion.identity);
            _playersDictionary[playerName] = player;

            Character character = player.GetComponent<Character>();
            if (character != null)
            {
                character.SetMapPath(_MapPath);
                hudManager.InitializePlayerHUD(playerName, characterId, character);
                Debug.Log($"HUD initialized for player {playerName}");
            }
        }
        else
        {
            Debug.LogError("Start node not found in the map path.");
        }
    }

    private void StartPlayerTurn()
    {
        if (_playerOrder == null || _playerOrder.Count == 0)
        {
            Debug.LogError("No players available for turn management");
            return;
        }

        _waitingForDiceThrow = true;
        _turnTimer = 0f;
        
        Debug.Log($"Starting turn for player index {_currentPlayerIndex}");
        hudManager.UpdateActivePlayer(_currentPlayerIndex);
        
        string currentPlayerName = _playerOrder[_currentPlayerIndex];

        if (boardGameCamera != null && _playersDictionary.TryGetValue(currentPlayerName, out GameObject playerGO))
        {
            boardGameCamera.SetTarget(playerGO.transform);
        }

        _RoomManager.SendToClient(currentPlayerName, "YOURTURN|15");
    }

    public void HandlePlayerAction(string playerName, string action)
    {
        if (action == "THROW_DICE" && IsPlayerTurn(playerName))
        {

            ThrowDice(playerName);

        }
        else if ((action == "YES_WELL" || action == "NO_WELL") && _waitingWellDecision)
        {
            if (action.StartsWith("YES"))
            {
                _RoomManager.SendToClient(playerName, "WELLDECISION|YES");
                HandleWellDecision(playerName, true);
            }
            else
            {
                _RoomManager.SendToClient(playerName, "WELLDECISION|NO");
                HandleWellDecision(playerName, false);
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
            string currentPlayerName = _playerOrder[_currentPlayerIndex];
            _RoomManager.SendToClient(currentPlayerName, $"DICETHROWN|{diceValue}");
            character.MoveOnMapPath(diceValue, () => {
                character.PathNodeEffect();
                EndTurn();
            });
        }
        
    }

    private void EndTurn()
    {
        if (_currentPlayerIndex == (_playerOrder.Count - 1))
        {
            _roundCount++;
            if (_roundCount >= MAX_ROUNDS)
            {
                EndGame();
                return;
            }
            _isMiniGameActive = true;
            BetweenRoundsQuickGame();
        }
        else
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _playerOrder.Count;
            StartPlayerTurn();
        }
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

    private void BetweenRoundsQuickGame()
    {
        int MAX_QUICKGAMES = 2;
        
        int quickgameNumber = Random.Range(1, MAX_QUICKGAMES + 1); // +1 Because the max is not inclusive
        

        _RoomManager.BroadcastToAll($"MINIGAMEID|{quickgameNumber}");
        MiniGamesManager.Instance.StartMiniGame(quickgameNumber, _playerOrder);
    }

    public void OnMiniGameFinished()
    {
        _isMiniGameActive = false;
        _currentPlayerIndex = 0; 
        StartPlayerTurn();
    }

    public void OnMiniGameCompleted(List<string> teamA, List<string> teamB)
    {
        Debug.Log($"Minijuego completado. Team A: {string.Join(", ", teamA)}, Team B: {string.Join(", ", teamB)}");
        _playerOrder.Clear();
        if(teamA.Count == 0 || teamB.Count == 0)
        {
            // TODO: FER ALGO SI VAN AL UNÍSONO
            _playerOrder.AddRange(teamA);
            _playerOrder.AddRange(teamB);
        }
        else if (teamA.Count > teamB.Count)
        {
            _playerOrder.AddRange(teamA);
            _playerOrder.AddRange(teamB);
        }
        else if (teamB.Count > teamA.Count)
        {
            _playerOrder.AddRange(teamB);
            _playerOrder.AddRange(teamA);
        }
        else
        {
            //TO DO: FER ALGO SI EMPATEN
            _playerOrder.AddRange(teamA);
            _playerOrder.AddRange(teamB);
        }

        hudManager.UpdatePlayerOrder(_playerOrder);
        
        _isMiniGameActive = false;
        _currentPlayerIndex = 0;
        StartPlayerTurn();
    }

    public void ShowWellDecisionPanel()
    {
        _waitingWellDecision = true;
        wellDecisionPanel.SetActive(true);
        // Envía mensaje al cliente web del jugador actual para mostrar la UI de votación
        string currentPlayerName = GetCurrentPlayer();
        if (!string.IsNullOrEmpty(currentPlayerName))
        {
            _RoomManager.SendToClient(currentPlayerName, "SHOW_WELL_UI");
        }
    }

    public void HandleWellDecision(string playerName, bool deposit)
    {
        if (_playersDictionary.TryGetValue(playerName, out GameObject player)){
            Character character = player.GetComponent<Character>();
            if(deposit){
                if (character != null)
                {
                    character.DepositResources();
                }
            }            
            _waitingWellDecision = false;
            wellDecisionPanel.SetActive(false);
            character.SetWaitingForWellDecision(false);
        }
    }

    private void EndGame()
{
    Debug.Log("¡La partida ha terminado!");

    string winner = null;
    float maxTotal = float.MinValue;

    foreach (var kvp in _playersDictionary)
    {
        var character = kvp.Value.GetComponent<Character>();
        if (character != null)
        {
            float total = character.GetLiquidOnWell() + character.GetLiquid();
            Debug.Log($"{kvp.Key}: Líquido total = {total}");
            if (total > maxTotal)
            {
                maxTotal = total;
                winner = kvp.Key;
            }
        }
    }

    Debug.Log($"¡El ganador es {winner} con {maxTotal} de líquido!");
    // Aquí puedes mostrar un panel de victoria, enviar mensaje a los clientes, etc.
    _isGameActive = false;
}
}
