using System;
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


        #region Behavior Variables (Not currently used due to change in design plan)
        //[Header("Behavior Graph")]
        //// Number of behaviors attached to the dog.
        //[SerializeField] private int behaviorNum = 0;

        //// List of Monobehaviors attached to the dog.
        //[SerializeField] private List<MonoBehaviour> behaviorComponents = new();

        //// List of movement behaviors that the dog can use.
        //private readonly List<IMovementBehavior> behaviors = new();
        #endregion

        // Safe Zone Position
        [SerializeField] private Vector3 safeZonePos;

        #region Movement Variables
        // Rigidbody component of the dog.
        private Rigidbody rb;
        public Rigidbody Rb => rb;

        // Maximum speed Dog can accelerate.
        private const float maxAcceleration = 24f;
        public float MaxAcceleration => maxAcceleration;

        // Maximum angular speed Dog can rotate.
        private const float maxAngularSpeed = 48f;
        public float MaxAngularSpeed => maxAngularSpeed;

        // Maximum angular acceleration Dog can rotate.
        private const float maxAngularAcceleration = 24f;
        public float MaxAngularAcceleration => maxAngularAcceleration;

        // Property returns current max speed of Dog.
        public float MaxSpeed => _maxSpeed;

        // Property returns currenty Y rotation of Dog in radians.
        public float YRot => AngleUtil.YawToAngleRad(transform.rotation);

        // Property returns current horizontal velocity of the dog.
        public Vector3 HorizVelocity => Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);

        // Target the dog is currently moving towards.
        [SerializeField] private Vector3 curTargetPos;

        // velocity to move the dog.
        [SerializeField] private Vector3 velocity;

        // foward transformation
        [SerializeField] private Vector3 forward;


        private const float turnRate = 5f;

        //// Accumulator for steering outputs from behaviors.
        //private SteeringOutput accumulator;

        #endregion

        #region Arive Behavior Variables
        //// Radius for arriving at target.
        //private const float targetRadius = 3.4f;

        //// Radius for slowing down when arriving at target.
        //private const float slowRadius = 7.5f;

        //// time to acheive max speed.
        //private const float timeToTarget = 0.1f;
        #endregion

        #region Vars to Track During Runtime
        // Dog will keep track of the locations he has seen a sheep and which sheep.
        private Dictionary<GameObject, Vector3> SheepFoundLocations = new Dictionary<GameObject, Vector3>();

        // List of sheep currently visible to the dog.
        private List<GameObject> sheepVisible = new List<GameObject>();

        // Is the dog currently near a sheep.
        private bool nearSheep = false;
        #endregion


        public void Awake()
        {
            // Find the layers in the project settings
            _targetsLayer = LayerMask.GetMask("Targets");
            _obstaclesLayer = LayerMask.GetMask("Obstacles");

            // Save reference to Rigidbody
            rb = GetComponent<Rigidbody>();

            // Freeze XZ rotation and Y position of Rigidbody
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            // Save Safe Zone Position
            // Since the Dog starts in the safe zone, we can just save his starting position as the safe zone position.
            

            //// Add active movement behaviors to the behaviors list
            //foreach (var mb in behaviorComponents)
            //{
            //    if (mb is IMovementBehavior b) behaviors.Add(b);
            //}

            //// Initialize the accumulator to zero
            //accumulator = SteeringOutput.Zero;
        }

        private void Start()
        {
            safeZonePos = transform.position;

            
        }

        private void Update()
        {
            //forward = transform.forward;
            // Do nothing if the dog is not active.
            if (!_isActive) return;
            
            Perception();
            DecisionMaking();
        }
        #region Perception
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
                // Check if the collider belongs to a sheep
                if (hitCollider.gameObject.GetComponent<Sheep>() != null)
                {
                    if (hitCollider.gameObject.GetComponent<Sheep>().InSafeZone && SheepFoundLocations.ContainsKey(hitCollider.gameObject))
                    {
                        SheepFoundLocations.Remove(hitCollider.gameObject);
                    }
                    else
                    {
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
                    }
                }
            }
            IsDogNearSheep();
        }

        /// <summary>
        /// Check if the dog is near a sheep.
        /// </summary>
        private void IsDogNearSheep()
        {
            // Check if Dog is near a sheep
            foreach (var sheep in sheepVisible)
            {
                //Debug.Log("Checking distance to sheep");
                if (Vector3.Distance(transform.position, sheep.transform.position) < 2.5f)
                {
                    Debug.Log("Dog is near a sheep");
                    nearSheep = true;
                    break;
                }
                else
                {
                    //Debug.Log("Dog is not near a sheep");
                    nearSheep = false;
                }
            }
            if (sheepVisible.Count == 0)
            {
                nearSheep = false;
            }
        }

        #endregion

        /// <summary>
        /// This method handles all decision making for the dog.
        /// </summary>
        private void DecisionMaking()
        {
            //Debug.Log("Starting DecisionMaking");

            //Debug.Log("nearSheep: " + nearSheep);

            // If Dog is near a sheep, walk to the safe zone
            if (nearSheep)
            {
                //Debug.Log("Dog is near a sheep, setting safe zone as current target position");
                curTargetPos = safeZonePos;
                return;
            }

            // Walk to the other side of the arena
            //Debug.Log("Walking to other side of arena");
            //Debug.Log(Mathf.Sign(transform.forward.z));
            //Debug.Log(Mathf.Sign(transform.forward.x));
            curTargetPos.x = -safeZonePos.x;
            curTargetPos.z = -safeZonePos.z;



            // If no sheep are visible, look for the farthest known location of a sheep
            if (sheepVisible.Count == 0 && SheepFoundLocations.Count > 0)
            {
                Vector3 farthestDist = Vector3.zero;
                foreach (var location in SheepFoundLocations.Values)
                {
                    if (Vector3.Distance(transform.position, location) > Vector3.Distance(transform.position, farthestDist))
                    {
                        farthestDist = location;
                    }
                }
                curTargetPos = farthestDist;
            }
            // If no known locations, search the arena
        }

        private void WalkFoward()
        {
            // Move the dog forward at max speed
            Vector3 forward = transform.forward * _maxSpeed;
            Vector3 velocity = new Vector3(forward.x, 0, forward.z);
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Make sure to use FixedUpdate for movement with physics based Rigidbody
        /// You can optionally use FixedDeltaTime for movement calculations, but it is not required since fixedupdate is called at a fixed rate
        /// </summary>
        private void FixedUpdate()
        {
            // Do nothing if the dog is not active.
            if (!_isActive) return;

            //// Reset accumulator each time movement is calculated
            //accumulator = SteeringOutput.Zero;

            //// Combine steering data from each behavior
            //foreach (var behavior in behaviors)
            //{
            //    var steering = behavior.GetSteering(this, curTargetPos);
            //    if (steering.Linear != Vector3.zero && behavior.OverrideSteering)
            //    {
            //        // If this behavior overrides steering, set the accumulator to this behavior's steering data and skip the rest
            //        accumulator = steering;
            //        break;
            //    }
            //    // Otherwise, accumulate the steering data
            //    accumulator.Linear += steering.Linear;
            //    accumulator.Angular += steering.Angular;
            //}

            //// Apply the accumulated steering to the Rigidbody
            //rb.AddForce(accumulator.Linear, ForceMode.Acceleration);
            //rb.AddTorque(Vector3.up * accumulator.Angular, ForceMode.Acceleration);


            // Calculate direction to the target position
            Vector3 targetDir = (curTargetPos - transform.position).normalized;

            // Calculate the movement vector
            velocity = targetDir * Mathf.Min(4.5f, Vector3.Distance(transform.position, curTargetPos));

            // Calculate the desired rotation towards the movement vector
            if (velocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity);

                // Smoothly rotate towards the target rotation based on the turn rate
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate);
            }

            // Move the Dog
            rb.linearVelocity = velocity;


            //// Clamp speed to max speed
            //Vector3 curVelocity = rb.linearVelocity;
            //Vector3 curHorizVelocity = Vector3.ProjectOnPlane(curVelocity, Vector3.up);
            //curHorizVelocity = Vector3.ClampMagnitude(curHorizVelocity, _maxSpeed);
            //rb.linearVelocity = curHorizVelocity + Vector3.up * curVelocity.y;

            //// Face the direction of movement if moving
            //if (curHorizVelocity.sqrMagnitude > 0.0001f)
            //{
            //    // Calculate the desired Y rotation based on current horizontal velocity
            //    float desiredYRot = Mathf.Atan2(curHorizVelocity.x, curHorizVelocity.z);

            //    // Get the current Y rotation of the dog
            //    float currentYRot = YRot;

            //    // Calculate the Y rotation error
            //    float YRotError = AngleUtil.MapToPi(desiredYRot - currentYRot);

            //    // Calculate turn rate
            //    float turnRate = Mathf.Min(Mathf.Abs(YRotError) / Time.fixedDeltaTime, maxAngularSpeed);

            //    // Calculate the turn
            //    float turn = Mathf.Sign(YRotError) * turnRate;

            //    // Calculate the torque to apply
            //    float torque = (turn - rb.angularVelocity.y) / Time.fixedDeltaTime;

            //    // Clamp the torque to max angular acceleration
            //    torque = Mathf.Clamp(torque, -maxAngularAcceleration, maxAngularAcceleration);

            //    // Apply the torque to the Rigidbody
            //    rb.AddTorque(Vector3.up * torque, ForceMode.Acceleration);
            //}
        }
    }
}
