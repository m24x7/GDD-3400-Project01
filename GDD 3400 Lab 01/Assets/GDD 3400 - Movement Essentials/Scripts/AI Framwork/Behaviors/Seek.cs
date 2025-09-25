// Behaviors/Seek.cs (dynamic)
using UnityEngine;
using AI.Core;

public class Seek : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    if (target == null) return SteeringOutput.Zero;
    Vector3 a = (target.position - agent.transform.position);
    a.y = 0f;
    if (a.sqrMagnitude < 0.0001f) return SteeringOutput.Zero;
    a = a.normalized * agent.maxAcceleration;
    return new SteeringOutput { linear = a, angular = 0f };
  }
}
