using System.Collections.Generic;
using Chris.PachiRogue.Core;
using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Wrapper around the character's Rigidbody2D: launch/reset, the mutator
    /// application pipeline, timed gravity effects, and rest detection. All
    /// physics manipulation happens through Rigidbody2D APIs in FixedUpdate
    /// paths; the squash/stretch visual lives on a child object and never
    /// touches the collider (CLAUDE.md hard rules).
    /// </summary>
    public sealed class PhysicsBody : MonoBehaviour, IPhysicsBodyHost
    {
        private const float RestSpeedThreshold = 0.15f;
        private const float RestTimeRequired = 1.25f;
        private const float SquashAmount = 0.06f;

        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private PhysicsMaterial2D _materialInstance;
        private Transform _visual;
        private float _visualBaseScale = 1f;
        private IEventBus _bus;

        private readonly List<PhysicsMutatorSO> _activeMutators = new List<PhysicsMutatorSO>();
        private float _baseGravityScale = 1f;
        private float _gravityOverrideRemaining;
        private float _gravityOverrideScale;
        private float _restTimer;
        private bool _launchActive;
        private float _killY = -100f;

        public PhysicsBodyContext Context { get; private set; }

        public bool LaunchActive => _launchActive;

        /// <summary>Builds a fully wired body GameObject at runtime (graybox — see ASSET_GUIDE.md for real art).</summary>
        public static PhysicsBody CreateRuntime(IEventBus bus, Vector2 position)
        {
            var go = new GameObject("Puni (PhysicsBody)");
            go.transform.position = position;

            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.gravityScale = 1f;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f;

            var body = go.AddComponent<PhysicsBody>();
            go.AddComponent<CollisionRouter>();
            body.Init(bus);
            return body;
        }

        public void Init(IEventBus bus)
        {
            _bus = bus;
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            // Never mutate a shared PhysicsMaterial2D asset — per-body instance.
            PhysicsMaterial2D source = _collider.sharedMaterial;
            _materialInstance = new PhysicsMaterial2D(name + " (material instance)")
            {
                bounciness = source != null ? source.bounciness : 0.45f,
                friction = source != null ? source.friction : 0.25f
            };
            _collider.sharedMaterial = _materialInstance;

            _baseGravityScale = _rigidbody.gravityScale;

            Context = new PhysicsBodyContext
            {
                Body = _rigidbody,
                MaterialInstance = _materialInstance,
                Bus = _bus,
                Host = this
            };

            CreateVisualChild();
            Sleep();
        }

        public void ApplyMutators(IEnumerable<PhysicsMutatorSO> mutators)
        {
            foreach (PhysicsMutatorSO mutator in mutators)
            {
                mutator.Apply(Context);
                _activeMutators.Add(mutator);
            }
        }

        public void RemoveAllMutators()
        {
            // Remove in reverse application order so scalars unwind cleanly.
            for (int i = _activeMutators.Count - 1; i >= 0; i--)
            {
                _activeMutators[i].Remove(Context);
            }

            _activeMutators.Clear();
        }

        /// <summary>Places the body at the launch point, dormant, awaiting Launch().</summary>
        public void ResetToLaunchPoint(Vector2 position, float killY)
        {
            _killY = killY;
            _rigidbody.position = position;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            Sleep();
        }

        public void Launch(Vector2 velocity)
        {
            _rigidbody.simulated = true;
            _rigidbody.linearVelocity = velocity;
            _restTimer = 0f;
            _launchActive = true;
            _bus.Publish(new LaunchStarted());
        }

        /// <summary>Force-resolves the launch (cup absorbed the body, or the run ends).</summary>
        public void ResolveNow()
        {
            if (!_launchActive)
            {
                return;
            }

            _launchActive = false;
            Sleep();
            _bus.Publish(new LaunchResolved());
        }

        public void SetTimedGravity(float gravityScale, float duration)
        {
            _gravityOverrideScale = gravityScale;
            _gravityOverrideRemaining = duration;
        }

        private void FixedUpdate()
        {
            if (!_launchActive)
            {
                return;
            }

            if (_gravityOverrideRemaining > 0f)
            {
                _gravityOverrideRemaining -= Time.fixedDeltaTime;
                _rigidbody.gravityScale = _gravityOverrideRemaining > 0f
                    ? _gravityOverrideScale
                    : _baseGravityScale;
            }

            if (_rigidbody.position.y < _killY)
            {
                ResolveNow();
                return;
            }

            // Rest detection: sustained near-zero speed ends the launch.
            if (_rigidbody.linearVelocity.sqrMagnitude < RestSpeedThreshold * RestSpeedThreshold)
            {
                _restTimer += Time.fixedDeltaTime;
                if (_restTimer >= RestTimeRequired)
                {
                    ResolveNow();
                }
            }
            else
            {
                _restTimer = 0f;
            }
        }

        private void Update()
        {
            if (_visual == null)
            {
                return;
            }

            // Squash & stretch on the visual child only — never the collider.
            Vector2 velocity = _rigidbody.simulated ? _rigidbody.linearVelocity : Vector2.zero;
            float speed = Mathf.Min(velocity.magnitude, 12f);
            float stretch = 1f + SquashAmount * speed;
            float squash = 1f / stretch;
            _visual.localScale = new Vector3(squash * _visualBaseScale, stretch * _visualBaseScale, 1f);
            if (speed > 0.5f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
                _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Sleep()
        {
            _rigidbody.gravityScale = _baseGravityScale;
            _gravityOverrideRemaining = 0f;
            _rigidbody.simulated = false;
        }

        private void CreateVisualChild()
        {
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = GrayboxSprites.Circle();
            renderer.color = new Color(1f, 0.62f, 0.78f, 1f); // Puni pink placeholder
            _visualBaseScale = _collider.radius * 2f;
            visualGo.transform.localScale = Vector3.one * _visualBaseScale;
            _visual = visualGo.transform;
        }
    }
}
