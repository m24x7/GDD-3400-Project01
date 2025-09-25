// Behaviors/KinematicArrive.cs
using UnityEngine;
using AI.Core;

public class KinematicArrive : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }
  public float satisfactionRadius = 0.25f;
  public float slowRadius = 3f;
  public float timeToTarget = 0.25f;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    if (!target) return SteeringOutput.Zero;
    Vector3 delta = target.position - agent.transform.position;
    delta.y = 0f;
    float dist = delta.magnitude;
    if (dist < satisfactionRadius) return SteeringOutput.Zero;

    float targetSpeed = (dist > slowRadius)
        ? agent.maxSpeed
        : agent.maxSpeed * (dist / Mathf.Max(slowRadius, 0.0001f));

    Vector3 desiredVel = (dist > 0f) ? delta * (targetSpeed / dist) : Vector3.zero;
    Vector3 accel = (desiredVel - agent.Body.linearVelocity) / Mathf.Max(0.0001f, timeToTarget);
    return new SteeringOutput { linear = accel, angular = 0f };
  }
}
