// Core/IMovementBehavior.cs
using UnityEngine;

namespace AI.Core
{
  public interface IMovementBehavior
  {
    public bool OverrideSteering { get; set; }
    
    SteeringOutput GetSteering(SteeringAgent agent, Transform target);
    // Optional: draw gizmos to help debug
    void DrawGizmos(SteeringAgent agent) { }
  }
}
