// Core/SteeringAgent.cs
using UnityEngine;
using System.Collections.Generic;

namespace AI.Core
{
  [RequireComponent(typeof(Rigidbody))]
  public class SteeringAgent : MonoBehaviour
  {
    [Header("Caps & Tuning")]
    public float maxSpeed = 8f;
    public float maxAcceleration = 24f;
    public float maxAngularSpeed = 48f;
    public float maxAngularAcceleration = 24f;
    public bool faceMovementDirection = true; // independent facing off by default

    [Header("Behavior Graph")]
    public List<MonoBehaviour> behaviorComponents = new(); // add components that implement IMovementBehavior

    // Cached
    public Rigidbody Body { get; private set; }
    public float OrientationRad => AngleUtil.YawToAngleRad(transform.rotation);
    public Vector3 VelocityXZ => Vector3.ProjectOnPlane(Body.linearVelocity, Vector3.up);

    // Internal
    private Transform _target;
    private readonly List<IMovementBehavior> _behaviors = new();
    private SteeringOutput _accumulator;

    void Awake()
    {
      Body = GetComponent<Rigidbody>();
      Body.interpolation = RigidbodyInterpolation.Interpolate;
      // Keep motion planar (common in “2.5D”): constrain XZ rotation, Y position as needed for your game
      Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
      foreach (var mb in behaviorComponents)
      {
        if (mb is IMovementBehavior b) _behaviors.Add(b);
      }
    }

    void FixedUpdate()
    {
      _accumulator = SteeringOutput.Zero;
      foreach (var b in _behaviors)
      {
        var s = b.GetSteering(this, _target);

        if (s.linear != Vector3.zero && b.OverrideSteering)
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
      Body.linearVelocity = vPlanar + Vector3.up * v.y;

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


    private void CreateCachedTarget()
    {
      // Destroy the cached target if it exists
      if (_target != null) Destroy(_target.gameObject);

      // Create a new cached target
      _target = Instantiate(new GameObject().transform, Vector3.zero, Quaternion.identity);

      // Set the name of the cached target
      _target.gameObject.name = this.gameObject.name + " [Target]";
    }

    public void SetTargetPosition(Vector3 target)
    {
      if (_target == null) CreateCachedTarget();

      _target.position = target;
    }

    public void SetTargetRotation(Quaternion target)
    {
      if (_target == null) CreateCachedTarget();

      _target.rotation = target;
    }

    public void SetTargetFromTransform(Transform target)
    {
      if (_target == null) CreateCachedTarget();
      _target.position = target.position;
      _target.rotation = target.rotation;
    }

    void OnDrawGizmosSelected()
    {
      if (!Application.isPlaying) return;
      foreach (var mb in behaviorComponents) if (mb is IMovementBehavior b) b.DrawGizmos(this);
    }
  }
}
