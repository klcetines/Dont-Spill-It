using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

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

[System.Serializable]
public class MatchAnswersQuestionsList
{
    public List<string> questions;
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

    [Header("Match Answers UI")]
    [SerializeField] private GameObject matchAnswersPanel;
    [SerializeField] private TMP_Text matchQuestionText;
    [SerializeField] private TMP_Text[] answerTexts;

    private List<string> matchQuestions = new List<string>();

    private string selectedPlayer;
    private string currentQuestion;
    private Dictionary<string, string> minigame2Answers = new Dictionary<string, string>();
    private Dictionary<string, int> minigame2Votes = new Dictionary<string, int>();
    private List<string> answerOrder = new List<string>();

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
                StartMatchAnswers(players);
                break;
            // Agrega más casos para otros minijuegos
            default:
                Debug.LogWarning("Minijuego no reconocido");
                break;
        }
    }

    private void LoadMatchAnswersQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("MatchAnswersQuestions");
        if (jsonFile != null)
        {
            var questionsList = JsonUtility.FromJson<MatchAnswersQuestionsList>(jsonFile.text);
            if (questionsList != null && questionsList.questions.Count > 0)
            {
                matchQuestions = questionsList.questions;
            }
            else
            {
                Debug.LogWarning("No se encontraron preguntas para Match Answers en el archivo JSON.");
            }
        }
        else
        {
            Debug.LogError("No se pudo cargar el archivo JSON de preguntas para Match Answers.");
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

    public void StartMatchAnswers(List<string> players)
    {
        if (matchQuestions.Count == 0)
            LoadMatchAnswersQuestions();

        selectedPlayer = players[UnityEngine.Random.Range(0, players.Count)];
        // Selecciona una pregunta aleatoria y reemplaza el marcador por el nombre del jugador
        string rawQuestion = matchQuestions[UnityEngine.Random.Range(0, matchQuestions.Count)];
        currentQuestion = rawQuestion.Replace("{player}", selectedPlayer);

        minigame2Answers.Clear();
        minigame2Votes.Clear();
        answerOrder.Clear();

        ShowMinigame2Question(currentQuestion, selectedPlayer);
    }

    // Llama esto cuando recibas una respuesta de un cliente web
    public void HandleMatchAnswers(string playerName, string answer)
    {
        var parts = answer.Split('|');
        if (parts.Length != 2) return;

        string actualAnswer = parts[1];
        minigame2Answers[playerName] = actualAnswer;

        // Envía confirmación al cliente HTML
        RoomManager.Instance.SendToClient(playerName, "MINIGAME2_ANSWER_RECEIVED");

        // Espera a que todos respondan
        if (minigame2Answers.Count == _currentPlayers.Count)
        {
            // Mezcla las respuestas y guarda el orden
            answerOrder = minigame2Answers.Values.OrderBy(x => UnityEngine.Random.value).ToList();

            // Muestra en Unity y envía a los clientes
            ShowMinigame2Choices(answerOrder);
            string[] letters = { "A", "B", "C", "D", "E", "F" };

            string choicesMsg = "MINIGAME2_CHOICES|" + string.Join("|", answerOrder.Count);
            foreach (var player in _currentPlayers)
            {
                if(player == selectedPlayer) continue;
                RoomManager.Instance.SendToClient(player, choicesMsg);
            }
        }
    }

    // Llama esto cuando recibas un voto de un cliente web
    public void HandleMinigame2Vote(string playerName, string message)
    {
        var parts = message.Split('|');
        if (parts.Length != 2) return;
        int answerIndex = parts[1].ToUpper()[0] - 'A'; // Convierte 'A'->0, 'B'->1, etc.
        minigame2Votes[playerName] = answerIndex;

        if (minigame2Votes.Count == _currentPlayers.Count - 1)
        {
            ShowMinigame2Results();
        }
    }

    private void ShowMinigame2Question(string question, string player)
    {
        if (matchAnswersPanel != null)
            matchAnswersPanel.SetActive(true);
        if (matchQuestionText != null)
            matchQuestionText.text = question;
    }
    
    // UI: Muestra todas las respuestas recibidas para votar
    private void ShowMinigame2Choices(List<string> choices)
    {
        string[] letters = { "A", "B", "C", "D", "E", "F" };
        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < choices.Count)
            {
                answerTexts[i].gameObject.SetActive(true);
                answerTexts[i].text = $"{letters[i]}. {choices[i]}";
            }
            else
            {
                answerTexts[i].gameObject.SetActive(false);
            }
        }
    }

     // UI: Muestra los resultados (puedes personalizar según tu lógica)
    private void ShowMinigame2Results()
    {
        // Ejemplo: muestra qué respuesta era la real y quién acertó
        int realIndex = answerOrder.FindIndex(ans => minigame2Answers[selectedPlayer] == ans);
        string realAnswer = minigame2Answers[selectedPlayer];

        string results = $"La respuesta real de {selectedPlayer} era:\n<b>{realAnswer}</b>\n\n";
        int aciertos = 0;
        foreach (var vote in minigame2Votes)
        {
            string player = vote.Key;
            int votedIndex = vote.Value;
            if (votedIndex == realIndex)
            {
                results += $"{player} acertó!\n";
                aciertos++;
            }
            else
            {
                results += $"{player} falló.\n";
            }
        }
        results += $"\nAciertos: {aciertos}/{minigame2Votes.Count}";
        if (matchQuestionText != null)
            matchQuestionText.text = results;

        // Oculta el panel tras unos segundos o cuando pulses un botón
        StartCoroutine(HideMatchAnswersPanelAfterDelay(4f));
        List<string> teamA = new List<string>();
        List<string> teamB = new List<string>();
        teamA.Add(selectedPlayer);

        foreach (var vote in minigame2Votes)
        {
            if (vote.Value == realIndex)
            teamA.Add(vote.Key);
            else
            teamB.Add(vote.Key);
        }

        GameManager.Instance.OnMiniGameCompleted(teamA, teamB);
    }

    private IEnumerator HideMatchAnswersPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (matchAnswersPanel != null)
        {
            // Limpia las respuestas
            foreach (var answerText in answerTexts)
            {
                answerText.text = "Answer";
                answerText.gameObject.SetActive(false);
            }

            // Limpia el texto de la pregunta
            if (matchQuestionText != null)
                matchQuestionText.text = "";

            // Limpia las colecciones
            minigame2Answers.Clear();
            minigame2Votes.Clear();
            answerOrder.Clear();

            // Oculta el panel
            matchAnswersPanel.SetActive(false);
        }
    }
}