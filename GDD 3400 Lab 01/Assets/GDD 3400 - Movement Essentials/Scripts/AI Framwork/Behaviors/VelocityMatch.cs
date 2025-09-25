// Behaviors/VelocityMatch.cs (match neighbor/team velocity)
using UnityEngine;
using AI.Core;

public class VelocityMatch : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }
  public Rigidbody neighbor;
  public float timeToTarget = 0.25f;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    if (!neighbor) return SteeringOutput.Zero;
    Vector3 desired = neighbor.linearVelocity - agent.Body.linearVelocity;
    desired.y = 0f;
    Vector3 accel = desired / Mathf.Max(0.0001f, timeToTarget);
    return new SteeringOutput { linear = accel, angular = 0f };
  }
}
