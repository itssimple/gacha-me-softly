using Chris.PachiRogue.Core;
using Chris.PachiRogue.Physics;
using NUnit.Framework;
using UnityEngine;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class MutatorTests
    {
        private GameObject _go;
        private PhysicsBodyContext _ctx;
        private PhysicsMaterial2D _sharedAsset;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MutatorTestBody");
            var body = _go.AddComponent<Rigidbody2D>();
            body.mass = 1f;

            _sharedAsset = new PhysicsMaterial2D("shared") { bounciness = 0.4f, friction = 0.2f };

            // Mirror PhysicsBody.Init: mutate a per-body instance, never the shared asset.
            var instance = new PhysicsMaterial2D("instance")
            {
                bounciness = _sharedAsset.bounciness,
                friction = _sharedAsset.friction
            };

            _ctx = new PhysicsBodyContext
            {
                Body = body,
                MaterialInstance = instance,
                Bus = new EventBus(),
                Host = new FakeHost()
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private sealed class FakeHost : IPhysicsBodyHost
        {
            public int TimedGravityCalls;

            public void SetTimedGravity(float gravityScale, float duration)
            {
                TimedGravityCalls++;
            }
        }

        [Test]
        public void BouncyAndHeavy_StackAndRemoveCleanly()
        {
            var bouncy = ScriptableObject.CreateInstance<BouncyBootsSO>();
            var heavy = ScriptableObject.CreateInstance<HeavyCoreSO>();

            float baseBounciness = _ctx.MaterialInstance.bounciness;
            float baseMass = _ctx.Body.mass;
            float baseDamage = _ctx.DamageScalar;

            bouncy.Apply(_ctx);
            heavy.Apply(_ctx);

            Assert.Greater(_ctx.MaterialInstance.bounciness, baseBounciness, "bouncy applied");
            Assert.Greater(_ctx.Body.mass, baseMass, "heavy mass applied");
            Assert.Greater(_ctx.DamageScalar, baseDamage, "heavy damage applied");

            heavy.Remove(_ctx);
            bouncy.Remove(_ctx);

            Assert.AreEqual(baseBounciness, _ctx.MaterialInstance.bounciness, 1e-4f, "bounciness restored");
            Assert.AreEqual(baseMass, _ctx.Body.mass, 1e-4f, "mass restored");
            Assert.AreEqual(baseDamage, _ctx.DamageScalar, 1e-4f, "damage restored");

            Object.DestroyImmediate(bouncy);
            Object.DestroyImmediate(heavy);
        }

        [Test]
        public void Mutation_NeverTouchesTheSharedMaterialAsset()
        {
            var bouncy = ScriptableObject.CreateInstance<BouncyBootsSO>();

            bouncy.Apply(_ctx);

            Assert.AreEqual(0.4f, _sharedAsset.bounciness, 1e-5f,
                "shared PhysicsMaterial2D asset must never be mutated");
            Assert.AreNotEqual(_sharedAsset.bounciness, _ctx.MaterialInstance.bounciness);

            bouncy.Remove(_ctx);
            Object.DestroyImmediate(bouncy);
        }

        [Test]
        public void MoonGravity_ReactsToPegHits_AndUnsubscribesOnRemove()
        {
            var moon = ScriptableObject.CreateInstance<MoonGravitySO>();
            var host = (FakeHost)_ctx.Host;

            moon.Apply(_ctx);
            _ctx.Bus.Publish(new PegHit { Value = 10 });
            _ctx.Bus.Publish(new PegHit { Value = 10 });
            Assert.AreEqual(2, host.TimedGravityCalls, "peg hits trigger timed gravity");

            moon.Remove(_ctx);
            _ctx.Bus.Publish(new PegHit { Value = 10 });
            Assert.AreEqual(2, host.TimedGravityCalls, "removed mutator must not react");
            Assert.IsEmpty(_ctx.StateBag, "state bag entry cleaned up");

            Object.DestroyImmediate(moon);
        }
    }
}
