using UnityEngine;

namespace GDD3400.Project01
{
    /// <summary>
    /// This interfaces defines the basic properties and methods for movement behaviors.
    /// </summary>
    public interface IMovementBehavior
    {
        /// <summary>
        /// Gets and sets a boolean indicating whether this behavior should override the dog's current steering output.
        /// </summary>
        public bool OverrideSteering { get; set; }

        /// <summary>
        /// Calculates the steering output required for the specified dog to move toward the target.
        /// </summary>
        /// <param name="dog">The dog for which the steering output is calculated. Cannot be null.</param>
        /// <param name="target">The target position that the dog should move toward. Cannot be null.</param>
        /// <returns>A <see cref="SteeringOutput"/> SteeringOutput containing the calculated linear and angular accelerations.</returns>
        SteeringOutput GetSteering(Dog dog, Transform target);
    }
}
