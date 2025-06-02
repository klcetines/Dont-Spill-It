using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    private PlayerHUD _playerHUD;
    private float _liquid = 0f;
    private float _health = 100f;
    private float _liquidOnWell = 0f;

    private MapPath _mapPath;
    private PathNode currentNode;

    bool diceUsed = false;

    private Vector3 _targetPosition { get; set; }
    private bool _isMoving { get; set; }
    private float moveSpeed = 2f;


    private Animator _animator;
    private Vector2 _velocity;
    private static readonly string X_SPEED = "XSpeed";
    private static readonly string Y_SPEED = "YSpeed";
    private static readonly string STILL_TRIGGER = "Still";

    private bool waitingForWellDecision = false;
    private bool wellDecisionDeposit = false;

    public void SetMapPath(MapPath mapPath){
        _mapPath = mapPath;
    }

    private void Start()
    {
        _targetPosition = transform.position;
        _isMoving = false;
        currentNode = _mapPath.GetFirstNode();
        _animator = GetComponent<Animator>();
        
        if (_animator == null)
        {
            Debug.LogError("Animator component not found on Character!");
        }
    }

    void Update()
    {
        // Mover al personaje si está en movimiento
        if (_isMoving)
        {
            MoveTowardsTarget();
        }
        else{
            _animator.SetTrigger(STILL_TRIGGER);
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

            AudioManager.Instance?.PlayWalkLoop();

            while (_isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
                {   
                    _animator.SetTrigger(STILL_TRIGGER);
                    _isMoving = false;
                }
                yield return null;
            }

            currentNode = node;

           if (node.GetNodeType() == PathNode.NodeType.Well)
            {
                AudioManager.Instance?.StopWalkLoop();
                waitingForWellDecision = true;
                GameManager.Instance.ShowWellDecisionPanel();

                while (waitingForWellDecision)
                    yield return null;
            }
        }
        AudioManager.Instance?.StopWalkLoop();
        if (_animator != null)
        {
            _animator.SetFloat(X_SPEED, 0f);
            _animator.SetFloat(Y_SPEED, 0f);
            _animator.SetTrigger(STILL_TRIGGER);
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
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        // Calculate velocity
        Vector3 movement = (transform.position - previousPosition) / Time.deltaTime;
        _velocity = new Vector2(movement.x, movement.y);

        // Update animator parameters
        if (_animator != null)
        {
            _animator.SetFloat(X_SPEED, _velocity.x);
            _animator.SetFloat(Y_SPEED, _velocity.y);
        }

        // Check if we reached the target
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            _isMoving = false;
            // Reset animator parameters when stopping
            if (_animator != null)
            {
                _animator.SetFloat(X_SPEED, 0f);
                _animator.SetFloat(Y_SPEED, 0f);
                _animator.SetTrigger(STILL_TRIGGER);
            }
        }
    }

    public void PathNodeEffect(){
        if(currentNode != null)
        {
            switch (currentNode.GetNodeType())
            {
                case PathNode.NodeType.Giver:
                    Debug.Log("Giver Node Effect");
                    AudioManager.Instance?.PlayGiver();
                    AddResources(10);
                    break;
                case PathNode.NodeType.Drainer:
                    Debug.Log("Drainer Node Effect");
                    AudioManager.Instance?.PlayDrainer();
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

    public void SetHUD(PlayerHUD hud)
    {
        _playerHUD = hud;
        UpdateHUDValues();
    }

    private void UpdateHUDValues()
    {
        if (_playerHUD != null)
        {
            _playerHUD.UpdateLiquid(_liquid);
            _playerHUD.UpdateHealth(_health);
        }
    }

    private void AddResources(int amount)
    {
        _liquid += amount;
        UpdateHUDValues();
    }

    private void RemoveResources(int amount)
    {
        _liquid = Mathf.Max(0, _liquid - amount);
        UpdateHUDValues();
        Debug.Log($"Removed {amount} resources. New total: {_liquid}");
    }

    public void DepositResources()
    {
        _liquidOnWell += _liquid;
        _liquid = 0;
        UpdateHUDValues();
        AudioManager.Instance?.PlayWell();
        Debug.Log($"Deposited resources. New total on well: {_liquidOnWell}");
    }

    public void SetWaitingForWellDecision(bool value)
    {
        waitingForWellDecision = value;
    }

    public float GetLiquidOnWell()
    {
        return _liquidOnWell;
    }

    public float GetLiquid()
    {
        return _liquid;
    }

}