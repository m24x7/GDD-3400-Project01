using UnityEngine;

namespace GDD3400.Project01
{
    public class Wander : MonoBehaviour, IMovementBehavior
    {
        [SerializeField] private bool overrideSteering = false;

        public bool OverrideSteering
        {
            get => overrideSteering;
            set => overrideSteering = value;
        }

        // Max random change in Y rotation per second in degrees.
        [SerializeField] private float maxYRotationPerSecond = 7.5f;

        // local wander Y rotation offset
        [SerializeField] private float wanderYRot;
        public SteeringOutput GetSteering(Dog dog, Transform target)
        {
            // Get random Y drift to wander
            float randYdrift = Random.Range(-maxYRotationPerSecond, maxYRotationPerSecond) * Mathf.Deg2Rad * Time.fixedDeltaTime;
            wanderYRot = AngleUtil.MapToPi(wanderYRot + randYdrift);

            // Calculate Dog's Y rotatioon in radians plus the wander offset
            float dogYRot = dog.YRot + wanderYRot;

            // Calculate the desired velocity based on the new Y rotation
            Vector3 desiredVel = new Vector3(Mathf.Sin(dogYRot), 0f, Mathf.Cos(dogYRot)) * dog.MaxSpeed;

            // Calculate the required acceleration to reach the desired velocity
            Vector3 requiredAccel = (desiredVel - dog.HorizVelocity) / 0.1f;

            // Return the steering output
            return new SteeringOutput { Linear = requiredAccel, Angular = 0f };
        }
    }
}
