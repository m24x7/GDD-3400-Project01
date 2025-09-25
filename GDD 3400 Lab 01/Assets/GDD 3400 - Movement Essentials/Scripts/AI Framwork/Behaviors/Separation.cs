// Behaviors/Separation.cs
using UnityEngine;
using AI.Core;
using System.Collections.Generic;

public class Separation : MonoBehaviour, IMovementBehavior
{
  [SerializeField] private bool _overrideSteering = false;
  public bool OverrideSteering
  {
    get => _overrideSteering;
    set => _overrideSteering = value;
  }

  [Header("Separation Settings")]
  [Tooltip("Radius to detect other steering agents")]
  public float separationRadius = 2f;
  
  [Tooltip("Strength of the separation force")]
  public float separationStrength = 5f;
  
  [Tooltip("Minimum distance to consider for separation")]
  public float minSeparationDistance = 0.5f;
  
  [Tooltip("Time to reach target velocity")]
  public float timeToTarget = 0.2f;

  [Header("Debug")]
  [Tooltip("Show separation radius in scene view")]
  public bool showDebugGizmos = true;

  public SteeringOutput GetSteering(SteeringAgent agent, Transform target)
  {
    // Find all steering agents within separation radius
    var nearbyAgents = FindNearbySteeringAgents(agent);
    
    if (nearbyAgents.Count == 0)
      return SteeringOutput.Zero;

    Vector3 separationForce = Vector3.zero;
    int validNeighbors = 0;

    foreach (var neighbor in nearbyAgents)
    {
      // Calculate distance to neighbor
      Vector3 toNeighbor = agent.transform.position - neighbor.transform.position;
      toNeighbor.y = 0f; // Keep movement on XZ plane
      float distance = toNeighbor.magnitude;

      // Skip if too close or too far
      if (distance < minSeparationDistance || distance > separationRadius)
        continue;

      // Calculate separation direction (away from neighbor)
      Vector3 separationDirection = toNeighbor.normalized;
      
      // Calculate separation strength based on distance (closer = stronger)
      float separationWeight = 1f - (distance / separationRadius);
      separationWeight = Mathf.Clamp01(separationWeight);
      
      // Project neighbor's velocity forward to predict future position
      Vector3 neighborVelocity = neighbor.Body.linearVelocity;
      Vector3 projectedPosition = neighbor.transform.position + neighborVelocity * timeToTarget;
      
      // Calculate direction to avoid projected position
      Vector3 toProjected = agent.transform.position - projectedPosition;
      toProjected.y = 0f;
      
      if (toProjected.sqrMagnitude > 0.0001f)
      {
        Vector3 avoidanceDirection = toProjected.normalized;
        
        // Combine separation and avoidance directions
        Vector3 combinedDirection = (separationDirection + avoidanceDirection).normalized;
        
        // Apply weight and strength
        separationForce += combinedDirection * separationWeight * separationStrength;
        validNeighbors++;
      }
    }

    if (validNeighbors == 0)
      return SteeringOutput.Zero;

    // Average the separation force
    separationForce /= validNeighbors;

    // Convert to acceleration
    Vector3 desiredVelocity = separationForce;
    Vector3 currentVelocity = agent.VelocityXZ;
    Vector3 acceleration = (desiredVelocity - currentVelocity) / Mathf.Max(0.0001f, timeToTarget);

    return new SteeringOutput { linear = acceleration, angular = 0f };
  }

  private List<SteeringAgent> FindNearbySteeringAgents(SteeringAgent agent)
  {
    var nearbyAgents = new List<SteeringAgent>();
    
    // Find all SteeringAgent components in the scene
    var allAgents = FindObjectsOfType<SteeringAgent>();
    
    foreach (var otherAgent in allAgents)
    {
      // Skip self
      if (otherAgent == agent)
        continue;
        
      // Check if within separation radius
      float distance = Vector3.Distance(agent.transform.position, otherAgent.transform.position);
      if (distance <= separationRadius)
      {
        nearbyAgents.Add(otherAgent);
      }
    }
    
    return nearbyAgents;
  }

  public void DrawGizmos(SteeringAgent agent)
  {
    if (!showDebugGizmos) return;
    
    // Draw separation radius
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(agent.transform.position, separationRadius);
    
    // Draw minimum separation distance
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(agent.transform.position, minSeparationDistance);
  }
}
