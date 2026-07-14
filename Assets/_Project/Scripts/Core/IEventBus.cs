using System;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Typed event bus for gameplay signals (see CLAUDE.md "Events over
    /// polling"). Events are structs to avoid per-publish allocations.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Publishes an event to all current subscribers of <typeparamref name="T"/>.</summary>
        void Publish<T>(T evt) where T : struct;

        /// <summary>
        /// Subscribes to events of type <typeparamref name="T"/>. Dispose the
        /// returned handle to unsubscribe.
        /// </summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }
}
