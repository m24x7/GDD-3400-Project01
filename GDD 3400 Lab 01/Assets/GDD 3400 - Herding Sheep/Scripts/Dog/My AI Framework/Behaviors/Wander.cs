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

        public SteeringOutput GetSteering(Dog dog, Transform target)
        {
            
            return SteeringOutput.Zero;
        }
    }
}
