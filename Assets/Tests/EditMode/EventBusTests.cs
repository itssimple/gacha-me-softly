using System;
using Chris.PachiRogue.Core;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class EventBusTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        private struct OtherEvent
        {
            public string Name;
        }

        [Test]
        public void Publish_ReachesSubscriber()
        {
            var bus = new EventBus();
            int received = 0;

            bus.Subscribe<TestEvent>(e => received = e.Value);
            bus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_ReachesAllSubscribers()
        {
            var bus = new EventBus();
            int count = 0;

            bus.Subscribe<TestEvent>(_ => count++);
            bus.Subscribe<TestEvent>(_ => count++);
            bus.Subscribe<TestEvent>(_ => count++);
            bus.Publish(new TestEvent());

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            var bus = new EventBus();

            Assert.DoesNotThrow(() => bus.Publish(new TestEvent()));
        }

        [Test]
        public void Publish_DoesNotReachSubscribersOfOtherEventTypes()
        {
            var bus = new EventBus();
            bool received = false;

            bus.Subscribe<OtherEvent>(_ => received = true);
            bus.Publish(new TestEvent());

            Assert.IsFalse(received);
        }

        [Test]
        public void DisposedSubscription_StopsReceiving()
        {
            var bus = new EventBus();
            int count = 0;

            IDisposable subscription = bus.Subscribe<TestEvent>(_ => count++);
            bus.Publish(new TestEvent());
            subscription.Dispose();
            bus.Publish(new TestEvent());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var bus = new EventBus();
            int count = 0;

            IDisposable subscription = bus.Subscribe<TestEvent>(_ => count++);
            bus.Subscribe<TestEvent>(_ => count++);

            subscription.Dispose();
            Assert.DoesNotThrow(() => subscription.Dispose());

            bus.Publish(new TestEvent());
            Assert.AreEqual(1, count, "Double-dispose must not remove another handler.");
        }

        [Test]
        public void UnsubscribingDuringPublish_DoesNotBreakIteration()
        {
            var bus = new EventBus();
            int secondHandlerCalls = 0;
            IDisposable first = null;

            first = bus.Subscribe<TestEvent>(_ => first.Dispose());
            bus.Subscribe<TestEvent>(_ => secondHandlerCalls++);

            Assert.DoesNotThrow(() => bus.Publish(new TestEvent()));
            Assert.AreEqual(1, secondHandlerCalls);

            // First handler removed itself; only the second remains.
            bus.Publish(new TestEvent());
            Assert.AreEqual(2, secondHandlerCalls);
        }

        [Test]
        public void SubscribingDuringPublish_DoesNotAffectCurrentPublish()
        {
            var bus = new EventBus();
            int lateHandlerCalls = 0;

            bus.Subscribe<TestEvent>(_ => bus.Subscribe<TestEvent>(_ => lateHandlerCalls++));
            bus.Publish(new TestEvent());

            Assert.AreEqual(0, lateHandlerCalls, "Handler added during publish must not receive the in-flight event.");
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            var bus = new EventBus();

            Assert.Throws<ArgumentNullException>(() => bus.Subscribe<TestEvent>(null));
        }
    }
}
