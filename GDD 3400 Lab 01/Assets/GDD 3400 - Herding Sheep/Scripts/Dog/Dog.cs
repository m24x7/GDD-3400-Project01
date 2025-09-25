using System.Collections.Generic;
using UnityEngine;

namespace GDD3400.Project01
{
    [RequireComponent(typeof(Rigidbody))]
    /// <summary>
    /// The Dog class is the main controller for the dog character in the game.
    /// This class acts as the brain for the dog, handling perception, decision making, and movement.
    /// </summary>
    public class Dog : MonoBehaviour
    {
        private bool _isActive = true;
        public bool IsActive 
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Required Variables (Do not edit!)
        static private float _maxSpeed = 5f;
        static private float _sightRadius = 7.5f;

        // Layers - Set In Project Settings
        private LayerMask _targetsLayer;
        private LayerMask _obstaclesLayer;

        // Tags - Set In Project Settings
        private static string friendTag = "Friend";
        private static string threatTag = "Threat";
        private static string safeZoneTag = "SafeZone";


        #region Behavior Variables
        [Header("Behavior Graph")]
        // Number of behaviors attached to the dog.
        [SerializeField] private int behaviorNum = 0;

        // List of Monobehaviors attached to the dog.
        [SerializeField] private List<MonoBehaviour> behaviorComponents = new();

        // List of movement behaviors that the dog can use.
        private readonly List<IMovementBehavior> behaviors = new();
        #endregion

        #region Movement Variables
        // Rigidbody component of the dog.
        private Rigidbody rb;
        public Rigidbody Rb => rb;

        // Property returns currenty Y rotation of Dog in radians.
        public float YRot => AngleUtil.YawToAngleRad(transform.rotation);

        // Property returns current horizontal velocity of the dog.
        public Vector3 HorizVelocity => Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);

        // Target the dog is currently moving towards.
        private Transform curTargetPos;

        // Accumulator for steering outputs from behaviors.
        private SteeringOutput accumulator;

        #endregion

        #region Vars to Track During Runtime
        // Dog will keep track of the locations he has seen a sheep and which sheep.
        private Dictionary<GameObject, Vector3> SheepFoundLocations = new Dictionary<GameObject, Vector3>();

        // List of sheep currently visible to the dog.
        private List<GameObject> sheepVisible = new List<GameObject>();
        #endregion


        public void Awake()
        {
            // Find the layers in the project settings
            _targetsLayer = LayerMask.GetMask("Targets");
            _obstaclesLayer = LayerMask.GetMask("Obstacles");

            // Save reference to Rigidbody
            rb = GetComponent<Rigidbody>();

            // Set Rigidbody interpolation to Interpolate
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Freeze XZ rotation of Rigidbody
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Add active movement behaviors to the behaviors list
            foreach (var mb in behaviorComponents)
            {
                if (mb is IMovementBehavior b) behaviors.Add(b);
            }

        }

        private void Update()
        {
            // Do nothing if the dog is not active.
            if (!_isActive) return;
            
            Perception();
            DecisionMaking();
        }

        /// <summary>
        /// This method handles all perception for the dog.
        /// </summary>
        private void Perception()
        {
            // Clear the list of visible sheep at the start of each perception cycle
            sheepVisible.Clear();

            // Use Physics.OverlapSphere to find all colliders within sight radius on the targets layer
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, _sightRadius, _targetsLayer);

            foreach (var hitCollider in hitColliders)
            {
                // Check if the collider belongs to a sheep (friendTag)
                if (hitCollider.CompareTag(friendTag))
                {
                    // Check for line of sight using Raycast
                    //Vector3 directionToSheep = (hitCollider.transform.position - transform.position).normalized;
                    //if (!Physics.Raycast(transform.position, directionToSheep, out RaycastHit hitInfo, _sightRadius, _obstaclesLayer))
                    //{
                        // If no obstacles are hit, the sheep is visible
                        sheepVisible.Add(hitCollider.gameObject);
                        // Update the last known location of the sheep
                        if (SheepFoundLocations.ContainsKey(hitCollider.gameObject))
                        {
                            SheepFoundLocations[hitCollider.gameObject] = hitCollider.transform.position;
                        }
                        else
                        {
                            SheepFoundLocations.Add(hitCollider.gameObject, hitCollider.transform.position);
                        }
                    //}
                }
            }
        }

        /// <summary>
        /// This method handles all decision making for the dog.
        /// </summary>
        private void DecisionMaking()
        {
            
        }

        /// <summary>
        /// Make sure to use FixedUpdate for movement with physics based Rigidbody
        /// You can optionally use FixedDeltaTime for movement calculations, but it is not required since fixedupdate is called at a fixed rate
        /// </summary>
        private void FixedUpdate()
        {
            // Do nothing if the dog is not active.
            if (!_isActive) return;

            // Reset accumulator each time movement is calculated
            accumulator = SteeringOutput.Zero;

            // Combine steering data from each behavior
            foreach (var behavior in behaviors)
            {
                var steering = behavior.GetSteering(this, curTargetPos);
                if (steering.Linear != Vector3.zero && behavior.OverrideSteering)
                {
                    // If this behavior overrides steering, set the accumulator to this behavior's steering data and skip the rest
                    accumulator = steering;
                    break;
                }
                // Otherwise, accumulate the steering data
                accumulator.Linear += steering.Linear;
                accumulator.Angular += steering.Angular;
            }

            // Apply the accumulated steering to the Rigidbody
            rb.AddForce(accumulator.Linear, ForceMode.Acceleration);
            rb.AddTorque(Vector3.up * accumulator.Angular, ForceMode.Acceleration);

            // Clamp speed to max speed
            Vector3 curVelocity = rb.linearVelocity;
            Vector3 curHorizVelocity = Vector3.ProjectOnPlane(curVelocity, Vector3.up);
            curHorizVelocity = Vector3.ClampMagnitude(curHorizVelocity, _maxSpeed);
            rb.linearVelocity = curHorizVelocity + Vector3.up * curVelocity.y;
        }
    }
}
