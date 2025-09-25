// Behaviors/Pursue.cs (seek predicted position)
using UnityEngine;
using AI.Core;

public class Pursue : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }

  public Rigidbody targetBody;
  public float maxPrediction = 1f;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    if (!target) return SteeringOutput.Zero;
    Vector3 delta = target.position - agent.transform.position; delta.y = 0f;
    float speed = agent.VelocityXZ.magnitude;
    float prediction = (speed <= 0.01f) ? 0f : Mathf.Min(maxPrediction, delta.magnitude / speed);
    Vector3 futurePos = target.position + (targetBody ? targetBody.linearVelocity * prediction : Vector3.zero);
    Vector3 desired = (futurePos - agent.transform.position); desired.y = 0f;
    if (desired.sqrMagnitude < 0.0001f) return SteeringOutput.Zero;
    return new SteeringOutput { linear = desired.normalized * agent.maxAcceleration };
  }
}
