using System;
using System.Collections.Generic;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Composition root for service lookup. A single instance is created by
    /// <see cref="GameBootstrap"/> in the Boot scene and handed to systems via
    /// constructor/init injection. There is deliberately no static access —
    /// see CLAUDE.md "No singletons".
    /// </summary>
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>Registers a service instance under the interface type <typeparamref name="T"/>.</summary>
        /// <exception cref="ArgumentNullException">If <paramref name="service"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If a service is already registered for <typeparamref name="T"/>.</exception>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (_services.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException(
                    $"A service is already registered for type {typeof(T).Name}.");
            }

            _services.Add(typeof(T), service);
        }

        /// <summary>Resolves the service registered for <typeparamref name="T"/>.</summary>
        /// <exception cref="InvalidOperationException">If nothing is registered for <typeparamref name="T"/>.</exception>
        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"No service registered for type {typeof(T).Name}.");
        }

        /// <summary>Attempts to resolve the service registered for <typeparamref name="T"/>.</summary>
        public bool TryResolve<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Returns true if a service is registered for <typeparamref name="T"/>.</summary>
        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>Removes all registrations. Used when tearing down a session (and by tests).</summary>
        public void Clear()
        {
            _services.Clear();
        }
    }
}
