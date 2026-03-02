using UnityEngine;

public class InvisibleBehaviour : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask obstacleMask;

    private InvisibleParameters parameters;

    private void Awake()
    {
        parameters = InvisibleParameters.Instance;

        if (parameters == null)
        {
            Debug.LogError("InvisibleParameters is missing. Disabling InvisibleBehaviour.");
            enabled = false;
        }
    }

    public bool CanSeeTarget()
    {
        if (parameters == null || target == null)
            return false;

        float viewDistance = parameters.ViewDistance;
        float viewAngle = parameters.BaseViewAngle;

        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, viewDistance))
        {
            return hit.transform == target;
        }

        return false;
    }
    private void OnDrawGizmos()
    {
        if (parameters == null)
            parameters = InvisibleParameters.Instance;

        if (parameters == null)
            return;

        float viewDistance = parameters.ViewDistance;
        float viewAngle = parameters.BaseViewAngle;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.color = Color.yellow;

        // Левая граница
        Vector3 leftBoundary = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward;
        Gizmos.DrawRay(origin, leftBoundary * viewDistance);

        // Правая граница
        Vector3 rightBoundary = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * forward;
        Gizmos.DrawRay(origin, rightBoundary * viewDistance);

        // Центральная линия
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, forward * viewDistance);
        int segments = 30;
        float halfAngle = viewAngle * 0.5f;

        Vector3 previousPoint = origin + (Quaternion.Euler(0f, -halfAngle, 0f) * forward) * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfAngle + (viewAngle / segments) * i;
            Vector3 nextPoint = origin + (Quaternion.Euler(0f, angle, 0f) * forward) * viewDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}