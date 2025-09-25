// Behaviors/Align.cs (angular arrive)
using UnityEngine;
using AI.Core;

public class Align : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }
  
  [Tooltip("Radians")]
  public float satisfactionAngle = 1f * Mathf.Deg2Rad;
  public float slowAngle = 45f * Mathf.Deg2Rad;
  public float timeToTarget = 0.2f;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    float targetYaw = target.eulerAngles.y * Mathf.Deg2Rad;
    float yawErr = AngleUtil.MapToPi(targetYaw - agent.OrientationRad);

    float absErr = Mathf.Abs(yawErr);
    if (absErr < satisfactionAngle) return SteeringOutput.Zero;

    float targetRot = (absErr > slowAngle)
      ? agent.maxAngularSpeed
      : agent.maxAngularSpeed * (absErr / Mathf.Max(slowAngle, 1e-4f));

    targetRot *= Mathf.Sign(yawErr);
    float angAccel = (targetRot - agent.Body.angularVelocity.y) / Mathf.Max(0.0001f, timeToTarget);
    return new SteeringOutput { angular = angAccel, linear = Vector3.zero };
  }
}
