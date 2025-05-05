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

    // Add these to the Character class
    private int resources = 0;


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

    public void PathNodeEffect(){
        if(currentNode != null)
        {
            switch (currentNode.GetNodeType())
            {
                case PathNode.NodeType.Giver:
                    Debug.Log("Giver Node Effect");
                    AddResources(10);
                    break;
                case PathNode.NodeType.Drainer:
                    Debug.Log("Drainer Node Effect");
                    RemoveResources(5);
                    break;
                case PathNode.NodeType.Well:
                    Debug.Log("Leave here your liquid Node Effect");
                    DepositResources();
                    break;
                default:
                    Debug.Log("Unknown Node Type");
                    break;
            }
        }
        else
        {
            Debug.LogError("Current Node is null, cannot apply effect.");
        }
    }

    private void AddResources(int amount)
    {
        resources += amount;
        Debug.Log($"Added {amount} resources. New total: {resources}");
    }

    private void RemoveResources(int amount)
    {
        resources = Mathf.Max(0, resources - amount);
        Debug.Log($"Removed {amount} resources. New total: {resources}");
    }

    private void DepositResources()
    {
        Debug.Log($"Deposited {resources} resources at well");
        resources = 0;
    }
}