// Core/SteeringAgent.cs
using UnityEngine;
using System.Collections.Generic;

namespace AI.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class SteeringAgent : MonoBehaviour
    {
        /// These vars prevent motion from growing out of control by capping speeds and accelerations.
        [Header("Caps & Tuning")]
        public float maxSpeed = 8f;
        public float maxAcceleration = 24f;
        public float maxAngularSpeed = 48f;
        public float maxAngularAcceleration = 24f;
        public bool faceMovementDirection = true; // independent facing off by default


        /// This list holds the behavior components that define the agent's steering logic.
        /// This is a list of MonoBehaviours, but at runtime we filter it into a different
        /// list that only includes those that implement IMovementBehavior.
        [Header("Behavior Graph")]
        public List<MonoBehaviour> behaviorComponents = new(); // add components that implement IMovementBehavior


        // Cached
        public Rigidbody Body { get; private set; } /// This var saves a reference to the agent's Rigidbody component.
        public float OrientationRad => AngleUtil.YawToAngleRad(transform.rotation); /// This var saves the Y rotation of the agent in radians.
        public Vector3 VelocityXZ => Vector3.ProjectOnPlane(Body.linearVelocity, Vector3.up); /// This var saves the horizontal velocity of the agent.


        // Internal
        private Transform _target;
        private readonly List<IMovementBehavior> _behaviors = new(); /// This list holds references to the active movement behaviors.
        private SteeringOutput _accumulator; /// This struct accumulates steering outputs from behaviors.

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); /// Save reference to the Rigidbody.
            Body.interpolation = RigidbodyInterpolation.Interpolate; /// Set rb interpolation to interpolate.
            // Keep motion planar (common in “2.5D”): constrain XZ rotation, Y position as needed for your game
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; /// The | operator is used to compountd the asignment of both constraints into one line of code.
            foreach (var mb in behaviorComponents)
            {
                if (mb is IMovementBehavior b) _behaviors.Add(b); /// You can check if a class implements an interface using "is"
            }
        }

        /// <summary>
        /// The steering loop is implemented in FixedUpdate
        /// </summary>
        void FixedUpdate()
        {
            _accumulator = SteeringOutput.Zero; /// Reset accumulator.
            foreach (var b in _behaviors) /// combine steering data from each behavior.
            {
                var s = b.GetSteering(this, _target);

                if (s.linear != Vector3.zero && b.OverrideSteering) /// if this behavior overrides steering, set the acumulator to this behavior's steering data and skip the rest.
                {
                    _accumulator = s;
                    break;
                }

                _accumulator.linear += s.linear;
                _accumulator.angular += s.angular;
            }

            // Clamp to caps
            if (_accumulator.linear.sqrMagnitude > 0f)
            {
                _accumulator.linear = Vector3.ClampMagnitude(_accumulator.linear, maxAcceleration);
            }
            _accumulator.angular = Mathf.Clamp(_accumulator.angular, -maxAngularAcceleration, maxAngularAcceleration);

            // --- Actuation: feed physics in FixedUpdate ---
            // Linear: apply as acceleration (independent of mass)
            Body.AddForce(_accumulator.linear, ForceMode.Acceleration);

            // Angular: apply torque around Y
            Body.AddTorque(Vector3.up * _accumulator.angular, ForceMode.Acceleration);

            // Clamp top speeds for stability
            var v = Body.linearVelocity;
            var vPlanar = Vector3.ProjectOnPlane(v, Vector3.up);
            vPlanar = Vector3.ClampMagnitude(vPlanar, maxSpeed);
            Body.linearVelocity = vPlanar + Vector3.up * v.y; /// This adds the clamped horizontal speed to the original vertical speed and sets it as the rb's linear velocity.

            Body.maxAngularVelocity = maxAngularSpeed; // Unity expects rad/s internally

            // Optionally face velocity
            if (faceMovementDirection && vPlanar.sqrMagnitude > 0.0001f)
            {
                var desiredYaw = Mathf.Atan2(vPlanar.x, vPlanar.z);
                var currentYaw = OrientationRad;
                var yawErr = AngleUtil.MapToPi(desiredYaw - currentYaw);
                // PD-ish snap: turn some of the remaining error this frame
                float turnRate = Mathf.Min(Mathf.Abs(yawErr) / Time.fixedDeltaTime, maxAngularSpeed);
                float turn = Mathf.Sign(yawErr) * turnRate;
                // convert desired angular velocity to torque as acceleration target
                float angAccel = Mathf.Clamp((turn - Body.angularVelocity.y) / Time.fixedDeltaTime, -maxAngularAcceleration, maxAngularAcceleration);
                Body.AddTorque(Vector3.up * angAccel, ForceMode.Acceleration);
            }
        }


        /// <summary>
        /// This function creates or recreates the target transform used by behaviors.
        /// </summary>
        private void CreateCachedTarget()
        {
            // Destroy the cached target if it exists
            if (_target != null) Destroy(_target.gameObject);

            // Create a new cached target
            _target = Instantiate(new GameObject().transform, Vector3.zero, Quaternion.identity);

            // Set the name of the cached target
            _target.gameObject.name = this.gameObject.name + " [Target]";
        }

        /// <summary>
        /// This function sets the position of the target transform used by behaviors.
        /// </summary>
        /// <param name="target"></param>
        public void SetTargetPosition(Vector3 target)
        {
            if (_target == null) CreateCachedTarget();

            _target.position = target;
        }

        /// <summary>
        /// This fuction sets the rotation of the target transform used by behaviors.
        /// </summary>
        /// <param name="target"></param>
        public void SetTargetRotation(Quaternion target)
        {
            if (_target == null) CreateCachedTarget();

            _target.rotation = target;
        }

        /// <summary>
        /// This function sets the target transform used by behaviors using a given transform.
        /// </summary>
        /// <param name="target"></param>
        public void SetTargetFromTransform(Transform target)
        {
            if (_target == null) CreateCachedTarget();
            _target.position = target.position;
            _target.rotation = target.rotation;
        }

        /// <summary>
        /// This function draws gizmos for each behavior that implements IMovementBehavior.
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            foreach (var mb in behaviorComponents) if (mb is IMovementBehavior b) b.DrawGizmos(this);
        }
    }
}
