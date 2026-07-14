using System;
using System.Collections.Generic;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Copy-on-write implementation of <see cref="IEventBus"/>: handler arrays
    /// are replaced on subscribe/unsubscribe, so publishing iterates a stable
    /// snapshot with no per-publish allocation, and handlers may safely
    /// subscribe/unsubscribe from within a publish callback.
    /// Plain C# — no UnityEngine dependency, fully unit-testable.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate[]> _handlers = new Dictionary<Type, Delegate[]>();

        public void Publish<T>(T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out Delegate[] snapshot))
            {
                return;
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                ((Action<T>)snapshot[i]).Invoke(evt);
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type type = typeof(T);
            if (_handlers.TryGetValue(type, out Delegate[] existing))
            {
                var grown = new Delegate[existing.Length + 1];
                Array.Copy(existing, grown, existing.Length);
                grown[existing.Length] = handler;
                _handlers[type] = grown;
            }
            else
            {
                _handlers[type] = new Delegate[] { handler };
            }

            return new Subscription(this, type, handler);
        }

        private void Unsubscribe(Type type, Delegate handler)
        {
            if (!_handlers.TryGetValue(type, out Delegate[] existing))
            {
                return;
            }

            int index = Array.IndexOf(existing, handler);
            if (index < 0)
            {
                return;
            }

            if (existing.Length == 1)
            {
                _handlers.Remove(type);
                return;
            }

            var shrunk = new Delegate[existing.Length - 1];
            Array.Copy(existing, 0, shrunk, 0, index);
            Array.Copy(existing, index + 1, shrunk, index, existing.Length - index - 1);
            _handlers[type] = shrunk;
        }

        private sealed class Subscription : IDisposable
        {
            private EventBus _bus;
            private readonly Type _type;
            private Delegate _handler;

            public Subscription(EventBus bus, Type type, Delegate handler)
            {
                _bus = bus;
                _type = type;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_bus == null)
                {
                    return;
                }

                _bus.Unsubscribe(_type, _handler);
                _bus = null;
                _handler = null;
            }
        }
    }
}
