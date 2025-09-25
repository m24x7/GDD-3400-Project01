using UnityEngine;

namespace GDD3400.Project01
{
    /// <summary>
    /// This struct holds the output data from steering behaviors.
    /// </summary>
    public class SteeringOutput
    {
        #region Vars
        // linear saves the desired acceleration (m/s^2) as a 3D vector.
        private Vector3 linear;

        // angular saves the desired angular acceleration (rad/s^2) as a float.
        private float angular;
        #endregion

        #region Properties
        // Getter and setter for linear
        public Vector3 Linear
        {
            get => linear;
            set => linear = value;
        }

        // Getter and setter for angular
        public float Angular
        {
            get => angular;
            set => angular = value;
        }

        // Static property that returns a SteeringOutput with zero linear and angular acceleration.
        public static SteeringOutput Zero => new SteeringOutput { linear = Vector3.zero, angular = 0f };
        #endregion
    }
}
