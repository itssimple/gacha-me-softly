using Chris.PachiRogue.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Drag-to-aim launcher: press anywhere, drag to set direction/power
    /// (slingshot style — drag away from the target), release to launch.
    /// EnhancedTouch on mobile, mouse fallback in editor/desktop/WebGL.
    /// Shows a sampled ballistic trajectory preview (first ~0.5 s) on a
    /// LineRenderer. Input is read in Update; all physics goes through
    /// PhysicsBody (FixedUpdate paths).
    /// </summary>
    public sealed class LauncherController : MonoBehaviour
    {
        private const int PreviewSamples = 14;
        private const float BasePreviewSeconds = 0.5f;
        private const float MaxLaunchSpeed = 16f;
        private const float DragToSpeed = 3.2f;

        private PhysicsBody _body;
        private Camera _camera;
        private LineRenderer _preview;

        private bool _aimingEnabled;
        private bool _dragging;
        private Vector2 _dragStartWorld;
        private Vector2 _pendingVelocity;
        private float _powerScale = 1f;
        private float _controlScale = 1f;

        public static LauncherController CreateRuntime(PhysicsBody body, Camera camera)
        {
            var go = new GameObject("Launcher");
            var launcher = go.AddComponent<LauncherController>();
            launcher._body = body;
            launcher._camera = camera;
            launcher.CreatePreview();
            EnhancedTouchSupport.Enable();
            return launcher;
        }

        /// <param name="powerScale">LaunchPower stat relative to base (1 = base).</param>
        /// <param name="controlScale">AimControl stat relative to base (extends preview).</param>
        public void SetStatScales(float powerScale, float controlScale)
        {
            _powerScale = Mathf.Max(0.1f, powerScale);
            _controlScale = Mathf.Max(0.1f, controlScale);
        }

        public void SetAimingEnabled(bool enabled)
        {
            _aimingEnabled = enabled;
            if (!enabled)
            {
                _dragging = false;
                _preview.enabled = false;
            }
        }

        private void Update()
        {
            if (!_aimingEnabled || _body == null)
            {
                return;
            }

            if (TryReadPointer(out Vector2 screenPosition, out bool pressed, out bool released))
            {
                Vector2 world = _camera.ScreenToWorldPoint(screenPosition);

                if (pressed && !_dragging)
                {
                    _dragging = true;
                    _dragStartWorld = world;
                }
                else if (_dragging)
                {
                    // Slingshot: velocity opposes the drag.
                    Vector2 drag = _dragStartWorld - world;
                    float maxSpeed = MaxLaunchSpeed * _powerScale;
                    _pendingVelocity = Vector2.ClampMagnitude(drag * DragToSpeed * _powerScale, maxSpeed);
                    UpdatePreview();

                    if (released)
                    {
                        _dragging = false;
                        _preview.enabled = false;
                        if (_pendingVelocity.magnitude > 1f)
                        {
                            _body.Launch(_pendingVelocity);
                        }
                    }
                }
            }
        }

        private bool TryReadPointer(out Vector2 position, out bool pressed, out bool released)
        {
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];
                position = touch.screenPosition;
                pressed = touch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                          touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                          touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary;
                released = touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                           touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                pressed = mouse.leftButton.isPressed;
                released = mouse.leftButton.wasReleasedThisFrame;
                return pressed || released || _dragging;
            }

            position = default;
            pressed = false;
            released = false;
            return false;
        }

        private void UpdatePreview()
        {
            _preview.enabled = true;
            Vector2 origin = _body.Context.Body.position;
            Vector2 gravity = Physics2D.gravity * _body.Context.Body.gravityScale;
            float previewSeconds = BasePreviewSeconds * _controlScale;

            _preview.positionCount = PreviewSamples;
            for (int i = 0; i < PreviewSamples; i++)
            {
                float t = previewSeconds * i / (PreviewSamples - 1);
                Vector2 point = origin + _pendingVelocity * t + 0.5f * gravity * (t * t);
                _preview.SetPosition(i, point);
            }
        }

        private void CreatePreview()
        {
            _preview = gameObject.AddComponent<LineRenderer>();
            _preview.material = new Material(Shader.Find("Sprites/Default"));
            _preview.widthMultiplier = 0.07f;
            _preview.startColor = new Color(1f, 0.95f, 0.7f, 0.9f);
            _preview.endColor = new Color(1f, 0.95f, 0.7f, 0.1f);
            _preview.enabled = false;
        }
    }
}
