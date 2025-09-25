// Core/SteeringOutput.cs
using UnityEngine;

namespace AI.Core
{
  /// Linear = desired acceleration (m/s^2), Angular = desired angular accel (rad/s^2)
  public struct SteeringOutput
  {
    public Vector3 linear;
    public float angular;
    public static SteeringOutput Zero => new SteeringOutput { linear = Vector3.zero, angular = 0f };
  }
}
