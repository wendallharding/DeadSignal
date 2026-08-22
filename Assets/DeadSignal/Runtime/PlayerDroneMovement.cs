using UnityEngine;

namespace DeadSignal
{
    public sealed class PlayerDroneMovement
    {
        public Vector3 Velocity { get; private set; }

        public Vector3 Tick(Vector2 input, float dt, PlayerDroneMovementTuning tuning)
        {
            if (dt <= 0f)
            {
                return Velocity;
            }

            input = Vector2.ClampMagnitude(input, 1f);
            var targetVelocity = new Vector3(input.x, 0f, input.y) * tuning.MaximumSpeed;
            var acceleration = _selectAcceleration(targetVelocity, tuning);
            Velocity = Vector3.MoveTowards(Velocity, targetVelocity, acceleration * dt);
            return Velocity;
        }

        public void ApplyResolvedVelocity(Vector3 velocity)
        {
            Velocity = new Vector3(velocity.x, 0f, velocity.z);
        }

        private float _selectAcceleration(Vector3 targetVelocity, PlayerDroneMovementTuning tuning)
        {
            if (targetVelocity.sqrMagnitude <= Mathf.Epsilon)
            {
                return tuning.Braking;
            }

            return Vector3.Dot(Velocity, targetVelocity) < 0f
                ? tuning.Acceleration * tuning.ReversalAccelerationMultiplier
                : tuning.Acceleration;
        }
    }
}
