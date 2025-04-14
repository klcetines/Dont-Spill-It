using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    private MapPath _mapPath;
    private PathNode currentNode;
    private string _playerName;

    int diceResult = -1;
    bool diceUsed = false;

    private Vector3 _targetPosition { get; set; } // Posición objetivo para el movimiento
    private bool _isMoving { get; set; } // Indica si el personaje está en movimiento
    private float moveSpeed = 5f;

    public void SetPlayerName(string name)
    {
        _playerName = name;
    }

    public void SetMapPath(MapPath mapPath){
        _mapPath = mapPath;
    }

    private void Start()
    {
        _targetPosition = transform.position;
        _isMoving = false;
        currentNode = _mapPath.GetFirstNode();
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
        return UnityEngine.Random.Range(1, 7);
    }

    public void MoveOnMapPath(int nDice, System.Action onComplete)
    {
        if (currentNode != null && _mapPath != null)
        {
            var nodes = _mapPath.GetChildNodesList();
            List<PathNode> path = new List<PathNode>();
            var targetNode = GetTargetNode(nodes, currentNode, nDice, ref path);
            
            StartCoroutine(MoveThroughPath(path, () => {
                Debug.Log("Moving");
                currentNode = targetNode;
                onComplete?.Invoke();
            }));
        }
        else{
            Debug.LogError("Current Node or MapPath Missing");
        }
    }
    
    private IEnumerator MoveThroughPath(List<PathNode> path, System.Action onComplete)
    {
        foreach (var node in path)
        {
            _targetPosition = node.transform.position;
            _isMoving = true;

            while (_isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
                {
                    _isMoving = false;
                }
                yield return null;
            }
        }
        onComplete?.Invoke();
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