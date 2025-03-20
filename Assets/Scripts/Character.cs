using UnityEngine;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    public WebSocketClient wsClient; // Referencia al cliente WebSocket
    public MapPath mapPath; 
    private PathNode currentNode;

    public float moveSpeed = 5f; // Velocidad de movimiento

    int diceResult = -1; 
    bool diceUsed = false; 

    private Vector3 _targetPosition { get; set; } // Posición objetivo para el movimiento
    private bool _isMoving { get; set; } // Indica si el personaje está en movimiento

    private void Start()
    {
        _targetPosition = transform.position; // Inicializa la posición objetivo
        _isMoving = false; // Inicializa el estado de movimiento
        diceResult = UnityEngine.Random.Range(1, 7);
        currentNode = mapPath.GetFirstNode();
    }

    void Update()
    {
        if(diceUsed){
            diceResult = UnityEngine.Random.Range(1, 7);
            diceUsed = false;
        }
    }

    public int ThrowDice()
    {
        diceUsed = true;
        return diceResult;
    }

    public void MoveOnMapPath(int nDice)
    {
        if (currentNode != null && mapPath != null)
        {
            // Obtener la lista de nodos hijos del camino
            var nodes = mapPath.GetChildNodesList();

            // Obtener el nodo objetivo según el resultado del dado
            var targetNode = GetTargetNode(nodes, currentNode, nDice);

            // Mover el personaje hacia el nodo objetivo
            MoveToNode(targetNode);
        }
        else
        {
            Debug.LogError("Error al mover el personaje: no se encontró el componente MapPath");
        }
    }

    private PathNode GetTargetNode(List<PathNode> nodes, PathNode startNode, int steps)
    {
        PathNode targetNode = startNode;

        for (int i = 0; i < steps; i++)
        {
            if (targetNode.nextNodes.Count > 0)
            {
                targetNode = targetNode.nextNodes[0];
            }
            else
            {
                break;
            }
        }

        return targetNode;
    }

    private void MoveToNode(PathNode targetNode)
    {
        if (targetNode != null)
        {
            _targetPosition = targetNode.transform.position; // Establecer la posición objetivo
            _isMoving = true; // Activar el movimiento
        }
    }

}
