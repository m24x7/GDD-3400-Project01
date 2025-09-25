// Core/AngleUtil.cs
using UnityEngine;

namespace AI.Core
{
    public static class AngleUtil
    {
        /// <summary>
        /// This function wraps a given angle in radians to [-π, π].
        /// This is useful for calculating shortest rotation direction/turn.
        /// </summary>
        /// <param name="angleRad"></param>
        /// <returns></returns>
        public static float MapToPi(float angleRad)
        {
            // While the angle (in radians) is > π, subtract 2π.
            // This goes until the angle is less than or equal to π (180 in degrees).
            while (angleRad > Mathf.PI) angleRad -= 2f * Mathf.PI;


            // While the angle (in radians) is < -π, add 2π.
            // This goes until the angle is greater than or equal to -π (-180 in degrees).
            while (angleRad < -Mathf.PI) angleRad += 2f * Mathf.PI;


            // This returns an angle in the range [-π, π].
            return angleRad;
        }

        /// <summary>
        /// This function takes a quaternion and
        /// extracts the yaw (rotation around Y) angle in radians.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static float YawToAngleRad(Quaternion q)
        {
            // project forward onto XZ (the "ground" plane).
            // This tells us where the object/angle is facing in the horizontal plane of the world space.
            // This ignores any vertical tilt (pitch) the object may have.
            Vector3 fwd = Vector3.ProjectOnPlane(q * Vector3.forward, Vector3.up).normalized;

            /* q * Vector3.forward is the object's forward direction in world space.
             * 
             * Vector3.ProjectOnPlane(..., Vector3.up) removes any vertical component (Y-axis)
             * aka projects the forward vector onto the horizontal plane.
             * 
             * .normalized makes it a unit vector (length of 1).
             * This removes the magnitude of the foward vector, leaving only the direction it is facing.
             */


            // This returns a 0 if forward is a zero vector (0,0,0).
            // This can happen if the object is looking straight up or down.
            // Normalizing a zero vector is undefined, however Unity returns (0,0,0) for this case.
            // We can't get any meaningful forward projection if the object is looking straight up or down,
            // so we return 0 radians in this case.
            if (fwd.sqrMagnitude < 1e-6f) return 0f;


            // This returns the yaw angle in radians.
            // Since we projected onto the XZ plane, we can use Atan2 with X and Z components.
            // This means that looking along positive Z is the foward direction (0 radians),
            // positive X is the right direction (π/2 radians),
            // negative X is the right direction (-π/2 radians),
            // and negative Z is the backward direction (±π radians).
            return Mathf.Atan2(fwd.x, fwd.z); // radians, 0 = +Z
        }
    }
}
