// Core/AngleUtil.cs
using UnityEngine;

namespace AI.Core
{
  public static class AngleUtil
  {
    public static float MapToPi(float angleRad)
    {
      while (angleRad > Mathf.PI) angleRad -= 2f * Mathf.PI;
      while (angleRad < -Mathf.PI) angleRad += 2f * Mathf.PI;
      return angleRad;
    }
    public static float YawToAngleRad(Quaternion q)
    {
      // project forward onto XZ
      Vector3 fwd = Vector3.ProjectOnPlane(q * Vector3.forward, Vector3.up).normalized;
      if (fwd.sqrMagnitude < 1e-6f) return 0f;
      return Mathf.Atan2(fwd.x, fwd.z); // radians, 0 = +Z
    }
  }
}
