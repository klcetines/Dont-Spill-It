using UnityEngine;
using System.Collections;
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
        if (diceUsed)
        {
            diceResult = UnityEngine.Random.Range(1, 7);
            diceUsed = false;
        }

        // Mover al personaje si está en movimiento
        if (_isMoving)
        {
            MoveTowardsTarget();
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
            
            List<PathNode> path = new List<PathNode>();
            
            // Obtener el nodo objetivo según el resultado del dado
            var targetNode = GetTargetNode(nodes, currentNode, nDice, ref path);

            // Mover el personaje hacia el nodo objetivo
            MoveToNode(path, targetNode);
            currentNode = targetNode;
        }
        else
        {
            Debug.LogError("Error al mover el personaje: no se encontró el componente MapPath");
        }
    }

    private PathNode GetTargetNode(List<PathNode> nodes, PathNode startNode, int steps, ref List<PathNode> path)
    {
        PathNode targetNode = startNode;

        for (int i = 0; i < steps; i++)
        {
            if (targetNode.nextNodes.Count > 0)
            {
                targetNode = targetNode.nextNodes[0];
                path.Add(targetNode);
            }
            else
            {
                break;
            }
        }

        return targetNode;
    }

    private void MoveToNode(List<PathNode> path, PathNode targetNode)
    {
        if (targetNode != null)
        {
            StartCoroutine(MoveThroughPath(path));
        }
    }

    private IEnumerator MoveThroughPath(List<PathNode> path)
    {
        foreach (var node in path)
        {
            _targetPosition = node.transform.position;
            _isMoving = true;

            // Wait until the character reaches the current node
            while (_isMoving)
            {
                yield return null;
            }
        }
    }

    private void MoveTowardsTarget()
    {
        // Mover al personaje hacia la posición objetivo
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        // Si el personaje llega a la posición objetivo, detener el movimiento
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            _isMoving = false;
        }
    }
}