using UnityEngine;
#if !UNITY_WEBGL
using UnityEngine.InputSystem;
#endif

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Optional mid-fall lateral nudge from device tilt (GAME_DESIGN.md §2:
    /// assist only, never required, off by default). Android-only: the
    /// #if !UNITY_WEBGL guard below is the gyro guard sanctioned by PLAN.md
    /// Phase 5; WebGL compiles it out and falls back to pointer input only.
    /// </summary>
    public sealed class GyroAssist : MonoBehaviour
    {
        [Tooltip("Off by default; enabled from settings later.")]
        [SerializeField] private bool assistEnabled;

        [Range(0f, 5f)]
        [SerializeField] private float strength = 1.5f;

        private PhysicsBody _body;

        public void Configure(PhysicsBody body)
        {
            _body = body;
        }

        public void SetAssistEnabled(bool enabled)
        {
            assistEnabled = enabled;
#if !UNITY_WEBGL
            if (enabled && AttitudeSensor.current != null)
            {
                InputSystem.EnableDevice(AttitudeSensor.current);
            }
#endif
        }

        private void FixedUpdate()
        {
#if !UNITY_WEBGL
            if (!assistEnabled || _body == null || !_body.LaunchActive)
            {
                return;
            }

            AttitudeSensor sensor = AttitudeSensor.current;
            if (sensor == null || !sensor.enabled)
            {
                return;
            }

            // Device roll → gentle lateral force. Assist, not steering.
            Quaternion attitude = sensor.attitude.ReadValue();
            float roll = Mathf.Clamp(attitude.eulerAngles.z > 180f
                ? attitude.eulerAngles.z - 360f
                : attitude.eulerAngles.z, -45f, 45f) / 45f;

            _body.Context.Body.AddForce(Vector2.right * (-roll * strength));
#endif
        }
    }
}
