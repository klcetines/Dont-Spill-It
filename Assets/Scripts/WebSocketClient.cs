using UnityEngine;
using WebSocketSharp;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    public GameObject player;

    private Character currentCharacter;

    void Start()
    {
        currentCharacter = player.GetComponent<Character>();
        if (currentCharacter == null)
        {
            Debug.LogError("Error al obtener el componente Character");
        }

        // Conectar al servidor WebSocket (cambia la URL si es necesario)
        ws = new WebSocket("ws://localhost:8080");

        // Evento cuando se abre la conexión
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Conectado al servidor WebSocket");
        };

        // Evento cuando se recibe un mensaje
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Mensaje recibido en Unity: " + e.Data);
            ProcesarMensaje(e.Data); // Procesar el mensaje recibido
        };

        // Evento cuando ocurre un error
        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Error en WebSocket: " + e.Message);
        };

        // Evento cuando se cierra la conexión
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Desconectado del servidor WebSocket");
        };

        // Conectar al servidor
        ws.Connect();
    }

    void ProcesarMensaje(string mensaje)
    {
        Debug.Log("Procesando mensaje: " + mensaje);

        try
        {
            if (mensaje == "throw")
            {
                int nDice = ThrowPlayerDice();
                EnviarMensaje("dice " + nDice);
                MovePlayer(nDice);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Excepción en ProcesarMensaje: " + ex.Message);
        }
    }

    public int ThrowPlayerDice()
    {
        Debug.Log("Recibido tirar Dado");

        if (currentCharacter != null)
        {
            int nDice = currentCharacter.ThrowDice();
            Debug.Log("Dado lanzado: " + nDice);
            return nDice;
        }
        Debug.Log("Error al lanzar el dado: no se encontró el componente Character");
        return -1;
    }

    private void MovePlayer(int nDice)
    {
        if (currentCharacter != null)
        {
            currentCharacter.MoveOnMapPath(nDice);
        }
        else
        {
            Debug.LogError("Error al mover el jugador: no se encontró el componente Character");
        }
    }

    public void EnviarMensaje(string mensaje)
    {
        if (ws != null && ws.IsAlive)
        {
            try
            {
                ws.Send(mensaje);
                Debug.Log("Mensaje enviado desde Unity: " + mensaje);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error al enviar mensaje: " + ex.Message);
            }
        }
        else
        {
            Debug.LogError("Error al enviar mensaje: WebSocket no está conectado");
        }
    }

    void OnDestroy()
    {
        // Cerrar la conexión WebSocket al destruir el objeto
        if (ws != null)
        {
            ws.Close();
        }
    }
}