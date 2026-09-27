using UnityEngine;

public class HorizontalCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float minX = -7f;
    [SerializeField] float maxX = 7f;
    [SerializeField] float followSpeed = 6f;

    public Transform Target => target;
    public float MinX => minX;
    public float MaxX => maxX;

    public void Configure(Transform newTarget, float leftBound, float rightBound)
    {
        target = newTarget;
        minX = leftBound;
        maxX = rightBound;
    }

    void LateUpdate()
    {
        if (target == null) return;
        var position = transform.position;
        float desired = Mathf.Clamp(target.position.x, minX, maxX);
        position.x = Mathf.Lerp(position.x, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        transform.position = position;
    }
}
