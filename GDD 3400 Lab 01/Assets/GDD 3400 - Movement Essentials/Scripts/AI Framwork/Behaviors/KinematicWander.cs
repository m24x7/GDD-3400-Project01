// Behaviors/KinematicWander.cs
using UnityEngine;
using AI.Core;

public class KinematicWander : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }
  [Tooltip("Max random change in yaw per second, degrees.")]
  public float maxRotationDegPerSec = 90f;
  [SerializeField] private float _wanderSpeedFactor = 0.4f;
  public float timeToTarget = 0.1f;

  private float _wanderYaw; // local offset we integrate

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    // Random yaw drift
    float deltaYaw = Random.Range(-maxRotationDegPerSec, maxRotationDegPerSec) * Mathf.Deg2Rad * Time.fixedDeltaTime;
    _wanderYaw = AngleUtil.MapToPi(_wanderYaw + deltaYaw);

    float agentYaw = agent.OrientationRad + _wanderYaw;
    Vector3 desiredVel = new Vector3(Mathf.Sin(agentYaw), 0f, Mathf.Cos(agentYaw)) * agent.maxSpeed * _wanderSpeedFactor;
    Vector3 accel = (desiredVel - agent.Body.linearVelocity) / Mathf.Max(0.0001f, timeToTarget);
    return new SteeringOutput { linear = accel, angular = 0f };
  }
}
