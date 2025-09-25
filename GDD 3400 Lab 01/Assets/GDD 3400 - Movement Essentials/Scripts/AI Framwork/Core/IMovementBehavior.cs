// Core/IMovementBehavior.cs
using UnityEngine;

namespace AI.Core
{
    /* Interfaces define a "contract" that a class must follow.
     * Interfaces allow for a child to inherit from multiple parents.
     * This is basically an abstract class that defines what methods
     * and properties a child class must implement, but doesn't define
     * what TYPE of class it is.
     *
     *
     * Interface vs Abstract Class:
     * Interface = "Job description"
     * Abstract Class = "Partially built machine"
     * 
     * 
     *
     * Yay polymorphism!
    */

    /// <summary>
    /// Defines the "contract" for movement behaviors that calculate
    /// steering outputs for agents.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface provide specific movement
    /// behaviors, such as seeking, fleeing, or path following.
    /// These behaviors calculate the desired steering output based
    /// on the agent's state and a target.
    /// </remarks>
    public interface IMovementBehavior
    {
        // This property states whether this behavior should
        // override the agent's current steering output.
        public bool OverrideSteering { get; set; }


        // This method returns the steering output of the behavior
        // using the agent and a target transform.
        SteeringOutput GetSteering(SteeringAgent agent, Transform target);


        // Optional: draw gizmos to help debug
        void DrawGizmos(SteeringAgent agent) { }
    }
}
