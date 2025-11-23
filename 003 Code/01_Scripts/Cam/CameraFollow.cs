using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followLerp = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    public bool IsEnabled { get; set; } = true;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsEnabled || target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followLerp);

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}