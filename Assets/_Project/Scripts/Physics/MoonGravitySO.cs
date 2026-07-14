using System;
using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Event-reactive mutator: every peg hit briefly lowers gravity, letting
    /// the body drift. Demonstrates the bus-subscribing mutator pattern —
    /// the subscription handle lives in the context's state bag so Remove can
    /// dispose it.
    /// </summary>
    [CreateAssetMenu(menuName = "PachiRogue/Mutators/Moon Gravity", fileName = "Mutator_MoonGravity")]
    public sealed class MoonGravitySO : PhysicsMutatorSO
    {
        [SerializeField] private float gravityScale = 0.4f;
        [SerializeField] private float duration = 3f;

        public override void Apply(PhysicsBodyContext ctx)
        {
            IDisposable subscription = ctx.Bus.Subscribe<PegHit>(
                _ => ctx.Host.SetTimedGravity(gravityScale, duration));
            ctx.StateBag[this] = subscription;
        }

        public override void Remove(PhysicsBodyContext ctx)
        {
            if (ctx.StateBag.TryGetValue(this, out object stored) && stored is IDisposable subscription)
            {
                subscription.Dispose();
                ctx.StateBag.Remove(this);
            }
        }
    }
}
