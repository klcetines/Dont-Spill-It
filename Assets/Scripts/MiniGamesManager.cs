using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class QuestionsList
{
    public List<QuestionData> questions;
}

[System.Serializable]
public class QuestionData
{
    public string text;
    public string optionA;
    public string optionB;
}

public class MiniGamesManager : MonoBehaviour
{
    private RoomManager _RoomManager;
    private MainThreadDispatcher _MainThreadDispatcher;

    private List<string> _currentPlayers;

    public static MiniGamesManager Instance { get; private set; }
    public event Action<List<string>> OnMiniGameFinished; // Puedes pasar ganadores o nuevo orden

    [Header("Would You Rather UI")]
    [SerializeField] private GameObject wouldYouRatherPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;

    private Dictionary<string, string> playerVotes = new Dictionary<string, string>();
    [SerializeField] private GameObject voteCirclePrefab;
    [SerializeField] private Transform optionAContainer;
    [SerializeField] private Transform optionBContainer;
    private Dictionary<string, GameObject> voteCircles = new Dictionary<string, GameObject>();


    [SerializeField] private float circleSpacing = 75f; // Espacio entre círculos

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() {
        _RoomManager = FindFirstObjectByType<RoomManager>();
        _MainThreadDispatcher = FindFirstObjectByType<MainThreadDispatcher>();

        if (_RoomManager == null || _MainThreadDispatcher == null) {
            Debug.LogError("One or more required components are missing.");
            return;
        }
    }

    // Llama esto desde GameManager entre rondas
    public void StartMiniGame(int miniGameId, List<string> players)
    {
        _currentPlayers = new List<string>(players); 
        Debug.Log($"Iniciando minijuego {miniGameId} para jugadores: {string.Join(",", players)}");
        switch (miniGameId)
        {
            case 1:
                StartWouldYouRather();
                break;
            case 2:
                break;
            // Agrega más casos para otros minijuegos
            default:
                Debug.LogWarning("Minijuego no reconocido");
                break;
        }
    }

    private void StartWouldYouRather()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("WouldYouRatherQuestions");
        if (jsonFile != null)
        {
            var questions = JsonUtility.FromJson<QuestionsList>(jsonFile.text);
            if (questions != null && questions.questions.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, questions.questions.Count);
                var randomQuestion = questions.questions[randomIndex];
                Debug.Log($"Pregunta seleccionada: {randomQuestion.text}");

                // Mostrar panel y asignar textos
                wouldYouRatherPanel.SetActive(true);
                questionText.text = randomQuestion.text;
                optionAText.text = randomQuestion.optionA;
                optionBText.text = randomQuestion.optionB;
            }
            else
            {
                Debug.LogWarning("No se encontraron preguntas en el archivo JSON.");
            }
        }
        else
        {
            Debug.LogError("No se pudo cargar el archivo JSON de preguntas.");
        }
    }

    public void HandleVote(string playerName, string message)
    {
        var parts = message.Split('|');
        if (parts.Length != 2) return;

        string vote = parts[1];
        playerVotes[playerName] = vote;

        Transform parent = vote == "A" ? optionAContainer : optionBContainer;
        
        // Instancia el nuevo círculo
        GameObject voteCircle = Instantiate(voteCirclePrefab, parent);
        voteCircles[playerName] = voteCircle;

        // Reposiciona todos los círculos
        RepositionVoteCircles(parent);

        Debug.Log($"Voto recibido: {playerName} votó {vote}");
        RoomManager.Instance.SendToClient(playerName, $"RECIEVED_VOTE|{vote}");

        if (playerVotes.Count >= _currentPlayers.Count)
        {
            StartCoroutine(FinishWouldYouRatherWithDelay(2f));
        }
    }

    private IEnumerator FinishWouldYouRatherWithDelay(float endGameDelay = 1f)
    {
        // Espera unos segundos para que los jugadores vean el resultado final
        yield return new WaitForSeconds(endGameDelay);
        
        // Ahora ejecuta la lógica de finalización
        FinishWouldYouRather();
    }

    private void FinishWouldYouRather()
    {
        List<string> teamA = new List<string>();
        List<string> teamB = new List<string>();

        foreach (var vote in playerVotes)
        {
            if (vote.Value == "A")
                teamA.Add(vote.Key);
            else if (vote.Value == "B")
                teamB.Add(vote.Key);
        }

        // Limpia la UI
        foreach (Transform child in optionAContainer) Destroy(child.gameObject);
        foreach (Transform child in optionBContainer) Destroy(child.gameObject);
        voteCircles.Clear();
        wouldYouRatherPanel.SetActive(false);

        // Notifica al GameManager con los equipos
        GameManager.Instance.OnMiniGameCompleted(teamA, teamB);

        // Limpia los votos para la próxima vez
        playerVotes.Clear();
        _currentPlayers.Clear();
    }

    private void RepositionVoteCircles(Transform container)
    {
        var circles = new List<GameObject>();
        foreach (Transform child in container)
        {
            circles.Add(child.gameObject);
        }

        int count = circles.Count;
        if (count == 0) return;

        // Calcula cuántas filas y columnas necesitamos
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
        
        for (int i = 0; i < count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;

            // Calcula la posición central de la cuadrícula
            float xOffset = (col - (gridSize - 1) / 2f) * circleSpacing;
            float yOffset = ((gridSize - 1) / 2f - row) * circleSpacing;

            circles[i].transform.localPosition = new Vector3(xOffset, yOffset, 0);
        }
    }
}