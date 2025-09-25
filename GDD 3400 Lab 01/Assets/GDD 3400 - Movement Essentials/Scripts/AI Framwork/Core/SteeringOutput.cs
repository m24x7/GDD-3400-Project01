// Core/SteeringOutput.cs
using UnityEngine;

namespace AI.Core
{
    /// <summary>
    /// This is a struct for holding the output data from steering behaviors.
    /// 
    /// linear holds the desired acceleration (m/s^2) as a 3D vector. (How fast to move)
    /// 
    /// angular holds the desired angular acceleration (rad/s^2) as a float. (How fast to rotate)
    /// angular is multiplied by Vector3.up to get torque around the Y axis.
    /// 
    /// Zero is a static property that returns a SteeringOutput with zero linear and angular acceleration.
    /// This is a property that creates and returns a "do nothing" SteeringOutput struct.
    /// 
    /// "=>" is shorthand for a property that only has a getter.
    /// </summary>
    public struct SteeringOutput
  {
    public Vector3 linear;
    public float angular;
    public static SteeringOutput Zero => new SteeringOutput { linear = Vector3.zero, angular = 0f };
  }
}
