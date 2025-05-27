using UnityEngine;

public class BoardGameCamera : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 2f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.z = offset.z;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}