using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Represents a custom extension lifecycle event that has no direct SK filter equivalent.
/// </summary>
public sealed class ExtensionEvent
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public HookEvent Event { get; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets additional event data.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ExtensionEvent"/>.
    /// </summary>
    public ExtensionEvent(HookEvent hookEvent, IReadOnlyDictionary<string, object>? data = null)
    {
        Event = hookEvent;
        Timestamp = DateTimeOffset.UtcNow;
        Data = data ?? new Dictionary<string, object>(StringComparer.Ordinal);
    }
}

/// <summary>
/// A simple in-process event bus for extension lifecycle events
/// that don't map directly to SK filters (SessionStart, SessionEnd, PreCompact, Notification).
/// </summary>
public sealed class ExtensionEventBus : IExtensionEventBus
{
    private readonly List<Action<ExtensionEvent>> _handlers = [];

    /// <inheritdoc/>
    public void Subscribe(Action<ExtensionEvent> handler)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (handler is null) throw new ArgumentNullException(nameof(handler));
#endif
        _handlers.Add(handler);
    }

    /// <inheritdoc/>
    public void Publish(ExtensionEvent extensionEvent)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(extensionEvent);
#else
        if (extensionEvent is null) throw new ArgumentNullException(nameof(extensionEvent));
#endif
        foreach (var handler in _handlers)
            handler(extensionEvent);
    }
}

/// <summary>
/// Interface for the extension event bus.
/// </summary>
public interface IExtensionEventBus
{
    /// <summary>
    /// Subscribes a handler to receive extension events.
    /// </summary>
    void Subscribe(Action<ExtensionEvent> handler);

    /// <summary>
    /// Publishes an extension event to all subscribers.
    /// </summary>
    void Publish(ExtensionEvent extensionEvent);
}
