using JD.SemanticKernel.Extensions.Hooks;

namespace JD.SemanticKernel.Extensions.Hooks.Tests;

public class ExtensionEventBusTests
{
    [Fact]
    public void Subscribe_And_Publish_DeliversEvent()
    {
        var bus = new ExtensionEventBus();
        ExtensionEvent? received = null;

        bus.Subscribe(e => received = e);
        bus.Publish(new ExtensionEvent(HookEvent.SessionStart));

        Assert.NotNull(received);
        Assert.Equal(HookEvent.SessionStart, received.Event);
    }

    [Fact]
    public void Publish_MultipleSubscribers_AllReceive()
    {
        var bus = new ExtensionEventBus();
        var count = 0;

        bus.Subscribe(_ => count++);
        bus.Subscribe(_ => count++);
        bus.Subscribe(_ => count++);

        bus.Publish(new ExtensionEvent(HookEvent.Notification));

        Assert.Equal(3, count);
    }

    [Fact]
    public void Publish_WithData_DataAccessible()
    {
        var bus = new ExtensionEventBus();
        ExtensionEvent? received = null;

        bus.Subscribe(e => received = e);
        bus.Publish(new ExtensionEvent(
            HookEvent.PreCompact,
            new Dictionary<string, object>(StringComparer.Ordinal) { ["reason"] = "context_full" }));

        Assert.NotNull(received);
        Assert.Equal("context_full", received.Data["reason"]);
    }

    [Fact]
    public void Subscribe_NullHandler_ThrowsArgumentNullException()
    {
        var bus = new ExtensionEventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe(null!));
    }

    [Fact]
    public void Publish_NullEvent_ThrowsArgumentNullException()
    {
        var bus = new ExtensionEventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Publish(null!));
    }

    [Fact]
    public void ExtensionEvent_HasTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var evt = new ExtensionEvent(HookEvent.SessionStart);
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(evt.Timestamp, before, after);
    }
}
