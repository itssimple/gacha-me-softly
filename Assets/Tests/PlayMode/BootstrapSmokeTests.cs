using System.Collections;
using Chris.PachiRogue.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Chris.PachiRogue.Tests.PlayMode
{
    public class BootstrapSmokeTests
    {
        private GameObject _bootstrapObject;

        [TearDown]
        public void TearDown()
        {
            if (_bootstrapObject != null)
            {
                Object.Destroy(_bootstrapObject);
                _bootstrapObject = null;
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_RegistersAllCoreServices()
        {
            _bootstrapObject = new GameObject("Bootstrap (test)");
            var bootstrap = _bootstrapObject.AddComponent<GameBootstrap>();

            yield return null; // Let Awake run.

            Assert.IsNotNull(bootstrap.Services, "ServiceRegistry was not created.");
            Assert.IsTrue(bootstrap.Services.IsRegistered<IRngService>(), "IRngService missing.");
            Assert.IsTrue(bootstrap.Services.IsRegistered<IEventBus>(), "IEventBus missing.");
            Assert.IsTrue(bootstrap.Services.IsRegistered<ISaveService>(), "ISaveService missing.");
        }

        [UnityTest]
        public IEnumerator Bootstrap_ServicesAreFunctional()
        {
            _bootstrapObject = new GameObject("Bootstrap (test)");
            var bootstrap = _bootstrapObject.AddComponent<GameBootstrap>();

            yield return null;

            IRngService rng = bootstrap.Services.Resolve<IRngService>();
            Assert.DoesNotThrow(() => rng.NextULong());

            IEventBus bus = bootstrap.Services.Resolve<IEventBus>();
            bool received = false;
            using (bus.Subscribe<TestPing>(_ => received = true))
            {
                bus.Publish(new TestPing());
            }

            Assert.IsTrue(received, "Event bus did not deliver a published event.");

            ISaveService save = bootstrap.Services.Resolve<ISaveService>();
            Assert.DoesNotThrow(() => save.TryLoad(out _));
        }

        private struct TestPing
        {
        }
    }
}
