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
        [SerializeField] private float maxYRotationPerSecond = 20f;
        [SerializeField] private float wanderYRot;

        [SerializeField] private float wanderOffset = 3f;
        [SerializeField] private float wanderRadius = 2f;
        #endregion

        #region Path Following Variables
        private Vector3 arenaTopLeft;
        private Vector3 arenaBottomRight;
        private float targetPointsSpacing;
        [SerializeField] private List<Vector3> targetPoints = new List<Vector3>();
        [SerializeField] private int curTargetPointIndex = 0;
        [SerializeField] private bool targetPointsInitialized = false;
        [SerializeField] private float lookAheadSecs = 0.5f;
        #endregion

        #region Anti-Stall Variables
        [SerializeField] private float stuckSpeedThreshold = 0.05f;
        [SerializeField] private float stuckTimeThreshold = 0.35f;
        [SerializeField] private float nudgeAccel = 8.0f;
        private float stuckTimer = 0f;
        #endregion


        #region Behavior Flags
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

            // Stop the Rigidbody from auto-sleeping when moving at slow speeds.
            rb.sleepThreshold = 0.0f;
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
                    GameObject sheep = hitCollider.gameObject;

                    if (sheep.GetComponent<Sheep>().InSafeZone || Vector3.Distance(sheep.transform.position, safeZonePos) <= 7.425f)
                    {
                        if (SheepFoundLocations.ContainsKey(hitCollider.gameObject)) SheepFoundLocations.Remove(hitCollider.gameObject);
                        sheepVisible.Remove(hitCollider.gameObject); // The sheeps are guaranteed to be in the Visible list if they are in the Safe Zone, so we need to remove them.
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

            //// Remove visible sheep from the directory when the dog is within 7 meters of the safe zone
            //if (Vector3.Distance(transform.position, safeZonePos) <= 7.45f)
            //{
            //    var safeSheep = new List<GameObject>();
            //    foreach (var sheep in sheepVisible)
            //    {
            //        SheepFoundLocations.Remove(sheep);
            //    }
            //    sheepVisible.Clear();
            //}

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

        /// <summary>
        /// This method builds a path of target points for the dog to follow when wandering the arena.
        /// This implementation assumes the arena is square and centered at the origin. (which it is in this project)
        /// </summary>
        private void PathBuilder()
        {
            // Clear any existing target points
            targetPoints.Clear();

            float safeZoneMag = Vector3.Magnitude(safeZonePos);
            arenaTopLeft = new Vector3(-safeZoneMag, 0, safeZoneMag);
            arenaBottomRight = new Vector3(safeZoneMag, 0, -safeZoneMag);

            float width = arenaBottomRight.x - arenaTopLeft.x;
            float height = arenaTopLeft.z - arenaBottomRight.z;

            targetPointsSpacing = 10.5f;

            int cols = Mathf.RoundToInt(width / targetPointsSpacing) + 1;
            int rows = Mathf.RoundToInt(height / targetPointsSpacing) + 1;

            float xSpacing = width / (cols - 1);
            float zSpacing = height / (rows - 1);

            for (int r = 0; r < rows; r++)
            {
                float Z = arenaBottomRight.z + r * zSpacing;
                
                bool reverse = (r % 2 == 1);
                if (!reverse)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float x = arenaTopLeft.x + c * xSpacing;
                        targetPoints.Add(new Vector3(x, 0f, Z));
                    }
                }
                else
                {
                    for (int c = cols - 1; c >= 0; c--)
                    {
                        float x = arenaTopLeft.x + c * xSpacing;
                        targetPoints.Add(new Vector3(x, 0f, Z));
                    }
                }

            }

            if (targetPoints.Count < 2)
            {
                targetPointsInitialized = false;
                return;
            }

            curTargetPointIndex = NearestPathTargetIndex(transform.position);
            targetPoints.Add(safeZonePos); // Add the safe zone as the last target point in the path.
            targetPointsInitialized = true;
        }

        /// <summary>
        /// This method finds the index of the nearest target point in the path to the given position and returns it.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int NearestPathTargetIndex(Vector3 pos)
        {
            int closestIndex = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < targetPoints.Count; i++)
            {
                float dist = Vector3.Distance(pos, targetPoints[i]);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
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
            if (nearSheep && sheepVisible.Count >= 2)
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
            
            PathTargetUpdate();

            // If no known locations, search the arena
            //if (SheepFoundLocations.Count == 0) Wander();
            //else isWandering = false;

            // Walk to the other side of the arena
            //Debug.Log("Walking to other side of arena");
            //Debug.Log(Mathf.Sign(transform.forward.z));
            //Debug.Log(Mathf.Sign(transform.forward.x));
            //if (!isEscorting) curTargetPos = new Vector3(-safeZonePos.x, 0f, -safeZonePos.z);

            //if (wallInfrontOfDog) curTargetPos = -curTargetPos;
        }


        private void Wander()
        {
            //if (!isWandering) isWandering = true;
            // Get random Y drift to wander
            float randYdrift = UnityEngine.Random.Range(-maxYRotationPerSecond, maxYRotationPerSecond) * Mathf.Deg2Rad * Time.deltaTime;
            wanderYRot = AngleUtil.MapToPi(wanderYRot + randYdrift);

            // Get foward horizontal direction of dog
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            // Calculate the center of the wander circle
            Vector3 wanderCenter = fwd * wanderOffset;
            
            // Calculate target on circle
            //Vector3 cirTarget = new Vector3(Mathf.Sin(wanderYRot), 0f, Mathf.Cos(wanderYRot)) * wanderRadius;
            Vector3 cirTarget = right * Mathf.Sin(wanderYRot) * wanderRadius + fwd * Mathf.Cos(wanderYRot) * wanderRadius;

            // Calculate the new target position on the wander circle
            curTargetPos = transform.position + wanderCenter + cirTarget;
        }

        /// <summary>
        /// This method updates the current target position to the next target point in the path if the dog is close enough to the current target point.
        /// </summary>
        private void PathTargetUpdate()
        {
            #region Attempt 1
            //if (!targetPointsInitialized) PathBuilder(); // If the path hasn't been initialized, initialize it.
            //if (targetPoints.Count < 2) return; // If there are no target points, do nothing.

            ////// Save current target point
            ////Vector3 targetPoint = targetPoints[curTargetPointIndex];

            ////// Save next target point
            ////Vector3 nextTargetPoint = new Vector3(targetPoint.x - transform.position.x,
            ////    0f,
            ////    targetPoint.z - transform.position.z);
            ////if (nextTargetPoint.magnitude <= passRadius) curTargetPointIndex = NextPathTargetIndex();

            //// Save current segment points
            //Vector3 pointA = targetPoints[curTargetPointIndex];
            //Vector3 pointB = targetPoints[NextPathTargetIndex()];
            //pointA.y = pointB.y = transform.position.y;

            //Vector3 toB = new Vector3(pointB.x - transform.position.x, 0f, pointB.z - transform.position.z);
            //if (toB.magnitude <= passRadius)
            //{
            //    curTargetPointIndex = NextPathTargetIndex();
            //    return;
            //}

            //// Predict future position of dog
            //Vector3 planarV = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            //Vector3 futurePos = transform.position + planarV * lookAheadSecs;

            //// Project future position onto current path segment
            //Vector3 lineAB = pointB - pointA;
            //lineAB.y = 0f;
            //float lineABLen2 = lineAB.sqrMagnitude;
            //if (lineABLen2 < 1e-6f)
            //{
            //    curTargetPointIndex = NextPathTargetIndex();
            //    return;
            //}

            //float t = Mathf.Clamp01(Vector3.Dot(futurePos - pointA, lineAB) / lineABLen2);

            //float len = Mathf.Sqrt(lineABLen2);
            //float lookAheadDist = Mathf.Max(0.5f, targetPointsSpacing * 0.2f);
            //float leadT = Mathf.Clamp01(lookAheadDist / Mathf.Max(len, 0.01f));

            //float uTarget = Mathf.Min(0.999f, t + leadT);
            //Vector3 target = pointA + lineAB * uTarget;
            //target.y = transform.position.y;

            //curTargetPos = target;

            //// If close enough to target point, switch to next target point
            //if (float.IsNaN(curTargetPos.x) || float.IsNaN(curTargetPos.y) || float.IsNaN(curTargetPos.z))
            //{
            //    curTargetPointIndex = NextPathTargetIndex();
            //}
            //if (float.IsFinite(curTargetPos.x) || float.IsFinite(curTargetPos.y) || float.IsFinite(curTargetPos.z))
            //{
            //    curTargetPointIndex = NextPathTargetIndex();
            //}
            #endregion

            #region Attempt 2
            //if (!targetPointsInitialized) PathBuilder();
            //if (targetPoints.Count < 2) return;

            //Vector3 A = targetPoints[curTargetPointIndex];
            //Vector3 B = targetPoints[NextPathTargetIndex()];
            //A.y = B.y = transform.position.y;

            //// === Project current position to segment param u in [0,1]
            //Vector3 AB = B - A; AB.y = 0f;
            //float ab2 = AB.sqrMagnitude;
            //if (ab2 < 1e-6f) { curTargetPointIndex = NextPathTargetIndex(); return; }

            //Vector3 AP = new Vector3(transform.position.x - A.x, 0f, transform.position.z - A.z);
            //float u = Mathf.Clamp01(Vector3.Dot(AP, AB) / ab2);

            //// === If we are essentially at B, advance segment and bail (next frame rebuilds)
            //if (u >= 0.995f) { curTargetPointIndex = NextPathTargetIndex(); return; }

            //// === Look-ahead target strictly inside A->B
            //float len = Mathf.Sqrt(ab2);
            //float lead = Mathf.Max(0.5f, targetPointsSpacing * 0.25f);   // ~25% of spacing works well
            //float leadU = Mathf.Clamp01(lead / Mathf.Max(len, 0.01f));

            //float uTarget = Mathf.Min(0.995f, u + leadU);         // never aim exactly at B
            //Vector3 target = A + AB * uTarget;
            //target.y = transform.position.y;

            //curTargetPos = target;

            //// Defensive: if something is ever NaN, step forward
            //if (!float.IsFinite(curTargetPos.x) || !float.IsFinite(curTargetPos.z))
            //    curTargetPointIndex = NextPathTargetIndex();
            #endregion

            #region Attempt 3
            //if (!targetPointsInitialized) PathBuilder();
            //if (targetPoints.Count < 2) return;

            //const float EPS = 1e-6f;   // tiny length guard
            //const float EDGE = 0.995f;  // treat u ≥ 0.995 as "at end"

            //// Try to resolve onto a valid segment in this call (handles overshoot / tiny segments)
            //for (int guard = 0; guard < targetPoints.Count; guard++)
            //{
            //    Vector3 A = targetPoints[curTargetPointIndex];
            //    Vector3 B = targetPoints[NextPathTargetIndex()];
            //    A.y = B.y = transform.position.y;

            //    Vector3 AB = B - A; AB.y = 0f;
            //    float ab2 = AB.sqrMagnitude;

            //    // Skip degenerate segments
            //    if (ab2 < EPS) { curTargetPointIndex = NextPathTargetIndex(); continue; }

            //    // Parametric position along A->B for current dog position
            //    Vector3 AP = new Vector3(transform.position.x - A.x, 0f, transform.position.z - A.z);
            //    float u = Mathf.Clamp01(Vector3.Dot(AP, AB) / ab2);

            //    // If we're at (or essentially at) B, advance immediately and test the next segment
            //    if (u >= EDGE) { curTargetPointIndex = NextPathTargetIndex(); continue; }

            //    // Lead target STRICTLY inside the segment
            //    float len = Mathf.Sqrt(ab2);
            //    float spacing = Mathf.Max(1f, targetPointsSpacing);                 // your grid spacing
            //    float lead = Mathf.Clamp(spacing * 0.25f, 0.5f, spacing * 0.9f); // ~25% of spacing
            //    float leadU = Mathf.Clamp01(lead / Mathf.Max(len, 0.01f));

            //    float uTarget = Mathf.Clamp(u + leadU, 0.05f, 0.95f); // never aim at exact ends

            //    Debug.Log($"[Grid] idx={curTargetPointIndex} u={u:0.000} uTarget={uTarget:0.000} pos={transform.position} target={curTargetPos}");

            //    Vector3 target = A + AB * uTarget;
            //    target.y = transform.position.y;

            //    curTargetPos = target;

            //    // Defensive: if something went NaN, advance and try again
            //    if (!float.IsFinite(curTargetPos.x) || !float.IsFinite(curTargetPos.z))
            //    {
            //        curTargetPointIndex = NextPathTargetIndex();
            //        continue;
            //    }
            //    return; // success this frame
            //}

            //// Fallback (shouldn't hit): aim at current node
            //curTargetPos = targetPoints[curTargetPointIndex];
            #endregion

            #region Attempt 4
            //if (!targetPointsInitialized) PathBuilder();
            //if (targetPoints.Count < 2) return;

            //const float EPS = 1e-6f;  // tiny length guard
            //const float EDGE = 0.995f; // treat u >= EDGE as "at B"
            //const float MIN_AU = 0.02f;  // minimum param advance so target is always ahead

            //// Resolve onto a valid segment right now (handles overshoot & tiny segments)
            //for (int guard = 0; guard < targetPoints.Count; guard++)
            //{
            //    Vector3 A = targetPoints[curTargetPointIndex];
            //    Vector3 B = targetPoints[NextPathTargetIndex()];
            //    A.y = B.y = transform.position.y;

            //    Vector3 AB = B - A; AB.y = 0f;
            //    float ab2 = AB.sqrMagnitude;
            //    if (ab2 < EPS) { curTargetPointIndex = NextPathTargetIndex(); continue; }

            //    // Param along A->B for current position
            //    Vector3 AP = new Vector3(transform.position.x - A.x, 0f, transform.position.z - A.z);
            //    float u = Mathf.Clamp01(Vector3.Dot(AP, AB) / ab2);

            //    // If at/over the end, advance NOW and try the next segment
            //    if (u >= EDGE) { curTargetPointIndex = NextPathTargetIndex(); continue; }

            //    // Compute a lead distance, adapt by speed so it doesn't collapse
            //    Vector3 vPlanar = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            //    float speed = vPlanar.magnitude;
            //    float len = Mathf.Sqrt(ab2);

            //    // at least 25% of spacing, or what you'd cover in lookAheadSecs; pick the larger
            //    float spacing = Mathf.Max(1f, targetPointsSpacing);
            //    float leadDist = Mathf.Max(spacing * 0.25f, speed * lookAheadSecs);
            //    float leadU = Mathf.Clamp01(leadDist / Mathf.Max(len, 0.01f));

            //    // ensure the target is strictly ahead of u, and also strictly inside the segment
            //    float uTarget = Mathf.Clamp(u + Mathf.Max(leadU, MIN_AU), MIN_AU, 0.98f);

            //    Debug.Log($"[Grid] idx={curTargetPointIndex} u={u:0.000} uTarget={uTarget:0.000} pos={transform.position} target={curTargetPos}");

            //    Vector3 target = A + AB * uTarget;
            //    target.y = transform.position.y;
            //    curTargetPos = target;

            //    // Defensive: if anything went NaN, step forward and retry
            //    if (!float.IsFinite(curTargetPos.x) || !float.IsFinite(curTargetPos.z))
            //    {
            //        curTargetPointIndex = NextPathTargetIndex();
            //        continue;
            //    }
            //    return; // success this frame
            //}

            //// Fallback (shouldn’t happen): aim at the current node
            //curTargetPos = targetPoints[curTargetPointIndex];
            #endregion

            /* 
             * Attempt 5 - Final
             * Practically an entirely different implementation, BUT IT WORKS!
             * ... I spent way too long on this.
             */

            if (!targetPointsInitialized) PathBuilder();
            if (targetPoints.Count < 2) return;

            const float EPS = 1e-6f;  // tiny length guard
            const float MIN_AHEAD = 0.02f;  // 2%: minimum param advance so target is always ahead
            const float EDGE = 1f - MIN_AHEAD; // advance when we're within last 2% of segment

            // Resolve onto a valid segment right now (handles overshoot & tiny segments)
            for (int guard = 0; guard < targetPoints.Count; guard++)
            {
                Vector3 A = targetPoints[curTargetPointIndex];
                Vector3 B = targetPoints[NextPathTargetIndex()];
                A.y = B.y = transform.position.y;

                Vector3 AB = B - A; AB.y = 0f;
                float ab2 = AB.sqrMagnitude;
                if (ab2 < EPS) { curTargetPointIndex = NextPathTargetIndex(); continue; }

                // Param along A->B for current position
                Vector3 AP = new Vector3(transform.position.x - A.x, 0f, transform.position.z - A.z);
                float u = Mathf.Clamp01(Vector3.Dot(AP, AB) / ab2);

                // If we're near the end already, advance immediately and re-evaluate
                if (u >= EDGE) { curTargetPointIndex = NextPathTargetIndex(); continue; }

                // Lead distance (speed-aware): at least 25% of spacing or what you'd travel in lookAheadSecs
                float len = Mathf.Sqrt(ab2);
                float spacing = Mathf.Max(1f, targetPointsSpacing); // or your gridSpacing field if you added one
                float speed = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude;
                float leadDist = Mathf.Max(spacing * 0.25f, speed * lookAheadSecs);
                float leadU = Mathf.Clamp01(leadDist / Mathf.Max(len, 0.01f));

                // Ensure target is strictly ahead of u and strictly inside (0, EDGE]
                float uTarget = u + Mathf.Max(leadU, MIN_AHEAD);
                if (uTarget > EDGE) uTarget = EDGE;                 // keep inside
                if (uTarget <= u) uTarget = Mathf.Min(EDGE, u + MIN_AHEAD); // never behind

                Vector3 target = A + AB * uTarget;
                target.y = transform.position.y;
                curTargetPos = target;

                // debug to verify ahead-ness:
                //Debug.Log($"[Grid] idx={curTargetPointIndex} u={u:0.000} uTarget={uTarget:0.000} pos={transform.position} target={curTargetPos}");

                return; // success this frame
            }

            // Fallback (shouldn't happen): aim at the current node
            curTargetPos = targetPoints[curTargetPointIndex];
        }

        /// <summary>
        /// Get the next path target index, wrapping around if necessary
        /// </summary>
        /// <returns></returns>
        private int NextPathTargetIndex() => (curTargetPointIndex + 1) % targetPoints.Count;

        /// <summary>
        /// This method projects a point onto a line segment defined by two points (segA and segB) and returns the projected point.
        /// We love linear algebra. :)
        /// </summary>
        /// <param name="segA"></param>
        /// <param name="segB"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static Vector3 ProjectPointOnPathSegment(Vector3 segA, Vector3 segB, Vector3 point)
        {
            Vector3 lineAB = segB - segA;
            float lineABLen2 = Vector3.SqrMagnitude(lineAB);
            if (lineABLen2 < 1e-6f) return segA;
            float t = Vector3.Dot(point - segA, lineAB) / lineABLen2;
            t = Mathf.Clamp01(t);
            return segA + lineAB * t;
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
                moveSpeed = 2.5f;
            }
            else
            {
                moveSpeed = _maxSpeed;
            }

            // Calculate direction to the target position
            Vector3 to = curTargetPos - transform.position;
            float dist = to.magnitude;
            Vector3 dir = dist > 1e-6f ? to / Mathf.Max(dist, 1e-6f) : Vector3.zero;

            // Ease in and out of turns
            //float t = Mathf.Clamp01(dist / slowRadius);
            //float eased = t * t * (3f - 2f * t); // Smoothstep easing
            //float targetSpeed = eased * moveSpeed;

            // Calculate the target velocity
            Vector3 targetVelocity = dir * moveSpeed;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);

            // Calculate the steering force needed to reach the target velocity
            Vector3 steering = (targetVelocity - planarVelocity) / Time.fixedDeltaTime;
            rb.AddForce(steering, ForceMode.Acceleration);

            // Calculate the desired rotation towards the movement vector
            if (targetVelocity.sqrMagnitude > 1e-6f)
            {
                //Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity);

                //// Smoothly rotate towards the target rotation based on the turn rate
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.fixedDeltaTime);

                var look = Quaternion.LookRotation(new Vector3(targetVelocity.x, 0f, targetVelocity.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRate * Time.fixedDeltaTime);
            }

            // Move the Dog
            //if (isWandering) rb.linearVelocity = wonderVelocity;
            //else
            //rb.linearVelocity = velocity;


            // Clamp speed to max speed
            Vector3 curVelocity = rb.linearVelocity;
            Vector3 curHorizVelocity = Vector3.ProjectOnPlane(curVelocity, Vector3.up);
            curHorizVelocity = Vector3.ClampMagnitude(curHorizVelocity, moveSpeed);
            rb.linearVelocity = curHorizVelocity; //+ Vector3.up * curVelocity.y;


            Vector3 vPlanar = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            if (vPlanar.magnitude < stuckSpeedThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckTimeThreshold && targetPointsInitialized && targetPoints.Count >= 2)
                {
                    // Apply a small nudge in the direction of the target to help get unstuck
                    Vector3 pointA = targetPoints[curTargetPointIndex];
                    Vector3 pointB = targetPoints[(curTargetPointIndex + 1) % targetPoints.Count];
                    Vector3 lineAB = new Vector3(pointB.x - pointA.x, 0f, pointB.z - pointA.z);
                    Vector3 nudgeDir = lineAB.sqrMagnitude > 1e-6f ? lineAB.normalized : transform.forward;
                    rb.AddForce(nudgeDir * nudgeAccel, ForceMode.Acceleration);
                    rb.WakeUp();
                    stuckTimer = 0f; // Reset the timer after nudging
                }
            }
            else
            {
                stuckTimer = 0f; // Reset the timer if the dog is moving
            }


            #region Debug from Pathfinding Attempts...
            //if (Time.frameCount % 50 == 0 && targetPointsInitialized && targetPoints.Count >= 2)
            //{
            //    int nextIdx = (curTargetPointIndex + 1) % targetPoints.Count;
            //    float distToNode = Vector3.Distance(
            //        new Vector3(transform.position.x, 0f, transform.position.z),
            //        new Vector3(targetPoints[nextIdx].x, 0f, targetPoints[nextIdx].z));
            //    Debug.Log($"[Grid] idx={curTargetPointIndex} pos={transform.position} curTarget={curTargetPos} vPlanar={vPlanar.magnitude:0.00} distToNext={distToNode:0.00}");
            //}

            //if (Time.frameCount % 50 == 0 && targetPointsInitialized && targetPoints.Count >= 2)
            //{
            //    Vector3 A = targetPoints[curTargetPointIndex];
            //    Vector3 B = targetPoints[(curTargetPointIndex + 1) % targetPoints.Count];
            //    Vector3 AB = new Vector3(B.x - A.x, 0f, B.z - A.z);
            //    Vector3 AP = new Vector3(transform.position.x - A.x, 0f, transform.position.z - A.z);
            //    float u = (AB.sqrMagnitude > 1e-6f) ? Mathf.Clamp01(Vector3.Dot(AP, AB) / AB.sqrMagnitude) : 0f;

            //    float distToB = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
            //                                     new Vector3(B.x, 0f, B.z));
            //    float vvPlanar = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude;

            //    Debug.Log($"[Grid] idx={curTargetPointIndex} u={u:0.000} distToB={distToB:0.00} pos={transform.position} target={curTargetPos} v={vvPlanar:0.00}");
            //}
            #endregion

        }
    }
}
