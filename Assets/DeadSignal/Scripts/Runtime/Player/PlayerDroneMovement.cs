using UnityEngine;

namespace DeadSignal.Player
{
    public sealed class PlayerDroneMovement
    {
        public Vector3 Velocity { get; private set; }

        public Vector3 Tick(
            Vector2 input,
            float dt,
            PlayerDroneMovementTuning tuning,
            float speedMultiplier = 1f,
            float accelerationMultiplier = 1f)
        {
            if (dt <= 0f)
            {
                return Velocity;
            }

            input = Vector2.ClampMagnitude(input, 1f);
            speedMultiplier = Mathf.Max(1f, speedMultiplier);
            accelerationMultiplier = Mathf.Max(1f, accelerationMultiplier);
            var targetVelocity = new Vector3(input.x, 0f, input.y) * (tuning.MaximumSpeed * speedMultiplier);
            var acceleration = _selectAcceleration(targetVelocity, tuning) * accelerationMultiplier;
            Velocity = Vector3.MoveTowards(Velocity, targetVelocity, acceleration * dt);
            return Velocity;
        }

        public static Vector2 CalculateCameraRelativeInput(
            Vector2 input,
            Vector3 cameraForward,
            Vector3 cameraRight)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward = cameraForward.sqrMagnitude > Mathf.Epsilon ? cameraForward.normalized : Vector3.forward;
            cameraRight = cameraRight.sqrMagnitude > Mathf.Epsilon ? cameraRight.normalized : Vector3.right;
            var worldDirection = cameraRight * input.x + cameraForward * input.y;
            return Vector2.ClampMagnitude(new Vector2(worldDirection.x, worldDirection.z), input.magnitude);
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
