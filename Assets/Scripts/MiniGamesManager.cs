using UnityEngine;
using System;
using System.Collections.Generic;

public class MiniGamesManager : MonoBehaviour
{
    public static MiniGamesManager Instance { get; private set; }
    public event Action<List<string>> OnMiniGameFinished; // Puedes pasar ganadores o nuevo orden

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Llama esto desde GameManager entre rondas
    public void StartMiniGame(int miniGameId, List<string> players)
    {
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
        // Aquí lanzas el minijuego correspondiente
        // Por ejemplo, puedes mostrar UI, enviar mensajes, etc.

        // Simulación: tras 3 segundos, termina el minijuego
        StartCoroutine(FakeMiniGameResult(players));
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
            string randomQuestion = questions.questions[randomIndex];
            Debug.Log($"Pregunta seleccionada: {randomQuestion}");
            // Aquí puedes mostrar la pregunta en la UI o manejarla según tu lógica
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

    // Simulación de minijuego asíncrono
    private System.Collections.IEnumerator FakeMiniGameResult(List<string> players)
    {
        yield return new WaitForSeconds(3f);
        // Simula que el orden se ha alterado (puedes cambiarlo por la lógica real)
        players.Reverse();
        // Notifica al GameManager que terminó el minijuego
        OnMiniGameFinished?.Invoke(players);
    }
}