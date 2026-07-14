using System;
using Chris.PachiRogue.Core;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class ServiceRegistryTests
    {
        [Test]
        public void Register_ThenResolve_ReturnsSameInstance()
        {
            var registry = new ServiceRegistry();
            var rng = new RngService(1UL);

            registry.Register<IRngService>(rng);

            Assert.AreSame(rng, registry.Resolve<IRngService>());
        }

        [Test]
        public void Resolve_Unregistered_Throws()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<InvalidOperationException>(() => registry.Resolve<IRngService>());
        }

        [Test]
        public void Register_Duplicate_Throws()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRngService>(new RngService(1UL));

            Assert.Throws<InvalidOperationException>(
                () => registry.Register<IRngService>(new RngService(2UL)));
        }

        [Test]
        public void Register_Null_Throws()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.Register<IRngService>(null));
        }

        [Test]
        public void TryResolve_ReflectsRegistrationState()
        {
            var registry = new ServiceRegistry();

            Assert.IsFalse(registry.TryResolve(out IRngService missing));
            Assert.IsNull(missing);

            var rng = new RngService(1UL);
            registry.Register<IRngService>(rng);

            Assert.IsTrue(registry.TryResolve(out IRngService found));
            Assert.AreSame(rng, found);
        }

        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRngService>(new RngService(1UL));
            registry.Register<IEventBus>(new EventBus());

            registry.Clear();

            Assert.IsFalse(registry.IsRegistered<IRngService>());
            Assert.IsFalse(registry.IsRegistered<IEventBus>());
        }
    }
}
