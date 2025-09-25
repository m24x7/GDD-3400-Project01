using UnityEngine;
using AI.Core;

public class Evade : Pursue, IMovementBehavior
{
  public new SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    var s = base.GetSteering(agent, target);
    return new SteeringOutput { linear = -s.linear, angular = 0f };
  }
}