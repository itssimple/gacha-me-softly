using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Keeps the launched body in view on the tall vertical stage. Purely
    /// visual (LateUpdate on the camera transform — the camera has no
    /// rigidbody, so the FixedUpdate rule does not apply).
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        private const float SmoothTime = 0.18f;

        private Transform _target;
        private float _topY;
        private float _bottomY;
        private float _velocity;

        public void Configure(Transform target, float topY, float bottomY)
        {
            _target = target;
            _topY = topY;
            _bottomY = bottomY;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var camera = GetComponent<Camera>();
            float halfHeight = camera != null ? camera.orthographicSize : 5f;
            float desired = Mathf.Clamp(_target.position.y, _bottomY + halfHeight, _topY - halfHeight * 0.25f);
            float y = Mathf.SmoothDamp(transform.position.y, desired, ref _velocity, SmoothTime);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
    }
}
