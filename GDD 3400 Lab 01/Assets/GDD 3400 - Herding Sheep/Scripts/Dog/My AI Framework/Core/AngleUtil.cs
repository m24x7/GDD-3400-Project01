using UnityEngine;

namespace GDD3400.Project01
{
    /// <summary>
    /// This class provides utility functions for working with angles.
    /// </summary>
    public class AngleUtil
    {
        /// <summary>
        /// This method wraps a given angle in radians to [-π, π].
        /// </summary>
        /// <param name="angleRad"></param>
        /// <returns></returns>
        public static float MapToPi(float angleRad)
        {
            while (angleRad > Mathf.PI) angleRad -= 2f * Mathf.PI;
            while (angleRad < -Mathf.PI) angleRad += 2f * Mathf.PI;
            return angleRad;
        }

        /// <summary>
        /// This method takes an object's rotation (quaternion) and finds the yaw (rotation around Y) angle in radians.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static float YawToAngleRad(Quaternion q)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(q * Vector3.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) return 0f;
            return Mathf.Atan2(fwd.x, fwd.z);
        }
    }
}
