using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 3f, -8f);

    [Header("Look")]
    public bool lookAtTarget = true;
    public Vector3 lookOffset = Vector3.zero;

    [Header("Smooth")]
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = desiredPosition;
        }

        if (lookAtTarget)
        {
            transform.LookAt(target.position + lookOffset);
        }
    }
}