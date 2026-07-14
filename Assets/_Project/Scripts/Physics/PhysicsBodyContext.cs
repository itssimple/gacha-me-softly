using System.Collections.Generic;
using Chris.PachiRogue.Core;
using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>Host services a mutator may call on the body it is attached to.</summary>
    public interface IPhysicsBodyHost
    {
        /// <summary>Temporarily overrides gravityScale; reverts after <paramref name="duration"/> seconds (FixedUpdate timed).</summary>
        void SetTimedGravity(float gravityScale, float duration);
    }

    /// <summary>
    /// Everything a <see cref="PhysicsMutatorSO"/> may touch when applied:
    /// the rigidbody, the body's own material instance (never the shared
    /// asset), the event bus, and a per-mutator state bag for subscriptions.
    /// </summary>
    public sealed class PhysicsBodyContext
    {
        public Rigidbody2D Body;
        public PhysicsMaterial2D MaterialInstance;
        public IEventBus Bus;
        public IPhysicsBodyHost Host;

        /// <summary>Scales peg/bumper values published by the CollisionRouter.</summary>
        public float DamageScalar = 1f;

        /// <summary>Per-mutator storage (keyed by mutator instance) for subscriptions etc.</summary>
        public readonly Dictionary<object, object> StateBag = new Dictionary<object, object>();
    }
}
