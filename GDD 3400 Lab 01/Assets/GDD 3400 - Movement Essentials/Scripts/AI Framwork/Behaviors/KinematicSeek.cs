// Behaviors/KinematicSeek.cs
using UnityEngine;
using AI.Core;

public class KinematicSeek : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }
  public float timeToTarget = 0.1f;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    if (!target) return SteeringOutput.Zero;
    Vector3 to = target.position - agent.transform.position;
    to.y = 0f;
    if (to.sqrMagnitude < 0.0001f) return SteeringOutput.Zero;

    // Desired velocity at maxSpeed in direction to target
    Vector3 desiredVel = to.normalized * agent.maxSpeed;

    // Accel needed to reach desiredVel in timeToTarget
    Vector3 accel = (desiredVel - agent.Body.linearVelocity) / Mathf.Max(0.0001f, timeToTarget);
    return new SteeringOutput { linear = accel, angular = 0f };
  }
}
