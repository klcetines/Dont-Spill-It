using UnityEngine;

public class GameManager : MonoBehaviour
{
    RoomManager _RoomManager;
    MainThreadDispatcher _MainThreadDispatcher;
    WebSocketClient _WebSocketClient;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _RoomManager = FindFirstObjectByType<RoomManager>();
        _MainThreadDispatcher = FindFirstObjectByType<MainThreadDispatcher>();
        _WebSocketClient = FindFirstObjectByType<WebSocketClient>();

        if (_RoomManager == null)
        {
            Debug.LogError("RoomManager not found in the scene.");
        }

        if (_MainThreadDispatcher == null)
        {
            Debug.LogError("MainThreadDispatcher not found in the scene.");
        }

        if (_WebSocketClient == null)
        {
            Debug.LogError("WebSocketClient not found in the scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
