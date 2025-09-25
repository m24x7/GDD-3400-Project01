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


        // Safe Zone Position
        [SerializeField] private Vector3 safeZonePos;

        #region Movement Variables
        // Rigidbody component of the dog.
        private Rigidbody rb;
        public Rigidbody Rb => rb;

        // Property returns current max speed of Dog.
        public float MaxSpeed => _maxSpeed;

        // Property returns currenty Y rotation of Dog in radians.
        public float YRot => AngleUtil.YawToAngleRad(transform.rotation);

        // Target the dog is currently moving towards.
        [SerializeField] private Vector3 curTargetPos;

        private float moveSpeed = _maxSpeed;

        // velocity to move the dog.
        [SerializeField] private Vector3 velocity;

        // foward transformation
        [SerializeField] private Vector3 forward;


        private const float turnRate = 48f;

        //// Accumulator for steering outputs from behaviors.
        //private SteeringOutput accumulator;

        #endregion

        #region Wander Behavior Variables
        // Max random change in Y rotation per second in degrees.
        private float maxYRotationPerSecond = 7.5f;
        private float wanderYRot;

        private Vector3 wonderVelocity = Vector3.zero;
        #endregion


        #region Behavior Flags
        private bool isWandering = false;
        private bool isEscorting = false;
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

            // Remove any sheep from the dictionary that are null (despawned because they made it to the Safe Zone)
            if (SheepFoundLocations.Count > 0)
            {
                var despawnedSheep = new List<GameObject>();
                foreach (var sheep in SheepFoundLocations)
                {
                    if (sheep.Key == null) despawnedSheep.Add(sheep.Key);
                }
                foreach (var sheep in despawnedSheep) SheepFoundLocations.Remove(sheep);
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
                if (Vector3.Distance(transform.position, sheep.transform.position) < 3.5f)
                {
                    //Debug.Log("Dog is near a sheep");
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

        #region Decision Making
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
                isEscorting = true;
                curTargetPos = safeZonePos;
                return;
            }
            else
            {
                isEscorting = false;
            }

            // If no sheep are visible, look for the farthest known location of a sheep
            if (sheepVisible.Count == 0 && SheepFoundLocations.Count > 0)
            {
                float farthestDist = -1f;
                Vector3 farthestPos = Vector3.zero;
                foreach (var location in SheepFoundLocations.Values)
                {
                    float testDist = Vector3.Distance(transform.position, location);
                    if (testDist > farthestDist)
                    {
                        farthestDist = testDist;
                        farthestPos = location;
                    }
                }
                curTargetPos = farthestPos;
                return;
            }
            // If no known locations, search the arena
            //else Wander();

            // Walk to the other side of the arena
            //Debug.Log("Walking to other side of arena");
            //Debug.Log(Mathf.Sign(transform.forward.z));
            //Debug.Log(Mathf.Sign(transform.forward.x));
            if (!isEscorting) curTargetPos = new Vector3(-safeZonePos.x, 0f, -safeZonePos.z);
        }


        private void Wander()
        {
            // Get random Y drift to wander
            float randYdrift = UnityEngine.Random.Range(-maxYRotationPerSecond, maxYRotationPerSecond) * Mathf.Deg2Rad * Time.deltaTime;
            wanderYRot = AngleUtil.MapToPi(wanderYRot + randYdrift);

            // Calculate Dog's Y rotatioon in radians plus the wander offset
            float dogYRot = YRot + wanderYRot;

            // Calculate the desired velocity based on the new Y rotation
            wonderVelocity = new Vector3(Mathf.Sin(dogYRot), 0f, Mathf.Cos(dogYRot)) * MaxSpeed;
        }
        #endregion

        /// <summary>
        /// Make sure to use FixedUpdate for movement with physics based Rigidbody
        /// You can optionally use FixedDeltaTime for movement calculations, but it is not required since fixedupdate is called at a fixed rate
        /// </summary>
        private void FixedUpdate()
        {
            // Do nothing if the dog is not active.
            if (!_isActive) return;

            if (isEscorting)
            {
                moveSpeed = 2.4f;
            }
            else
            {
                moveSpeed = _maxSpeed;
            }

            // Calculate direction to the target position
            Vector3 targetDir = (curTargetPos - transform.position).normalized;

            // Calculate the movement vector
            velocity = targetDir * Mathf.Min(moveSpeed, Vector3.Distance(transform.position, curTargetPos));

            // Calculate the desired rotation towards the movement vector
            if (velocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity);

                // Smoothly rotate towards the target rotation based on the turn rate
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.fixedDeltaTime);
            }

            // Move the Dog
            if (isWandering) rb.linearVelocity = wonderVelocity;
            else rb.linearVelocity = velocity;


            //// Clamp speed to max speed
            //Vector3 curVelocity = rb.linearVelocity;
            //Vector3 curHorizVelocity = Vector3.ProjectOnPlane(curVelocity, Vector3.up);
            //curHorizVelocity = Vector3.ClampMagnitude(curHorizVelocity, _maxSpeed);
            //rb.linearVelocity = curHorizVelocity + Vector3.up * curVelocity.y;
        }
    }
}
