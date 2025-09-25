// Behaviors/ObstacleAvoidance.cs
using UnityEngine;
using AI.Core;

public class ObstacleAvoidance : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }

    public enum AvoidanceMethod
  {
    SphereCast,
    TrippleRaycast,
    SingleRaycast,
  }

  public AvoidanceMethod avoidanceMethod = AvoidanceMethod.SphereCast;
  public float lookAhead = 3f;
  public float avoidForce = 20f;
  public float agentRadius = 1.25f;
  public LayerMask obstacleMask;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    Vector3 origin = agent.transform.position + Vector3.up * 0.5f; // adjust for your colliders
    Collider[] hitColliders = Physics.OverlapSphere(origin, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore);
    if (hitColliders.Length > 0)
    {
        Vector3 tangentSteering = Vector3.zero;
        foreach (var hitCollider in hitColliders)
        {
            Vector3 hitPoint = hitCollider.ClosestPoint(origin);
            Vector3 toHitPoint = hitPoint - origin;
            Vector3 tangent = Vector3.Cross(toHitPoint, Vector3.up).normalized;
            tangentSteering += tangent;
        }
        tangentSteering.Normalize();
        return new SteeringOutput { linear = tangentSteering * avoidForce, angular = 0f };
    }

    RaycastHit hit;
    if (Physics.SphereCast(origin, agentRadius, agent.transform.forward, out hit, lookAhead, obstacleMask, QueryTriggerInteraction.Ignore))
    {
        Vector3 hitPoint = hit.point;
        Vector3 toHitPoint = hitPoint - origin;
        Vector3 tangent = Vector3.Cross(toHitPoint, Vector3.up).normalized;
        return new SteeringOutput { linear = tangent * avoidForce, angular = 0f };
    }

    return SteeringOutput.Zero;
  }


  

  public void DrawGizmos(SteeringAgent agent)
  {
    Gizmos.color = Color.cyan;
    Vector3 origin = agent.transform.position + Vector3.up * 0.5f;
    Vector3 dir = (agent.VelocityXZ.sqrMagnitude > 0.01f ? agent.VelocityXZ.normalized : agent.transform.forward) * lookAhead;
    Gizmos.DrawLine(origin, origin + dir);

  Gizmos.color = Color.red * Color.yellow;
  DebugHelpers.DrawGizmoCircle(origin, Vector3.up, agentRadius, Color.red);
  }
}
