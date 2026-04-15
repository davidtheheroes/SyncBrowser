using SyncBrowser.Core.Enums;

namespace SyncBrowser.Core.Models;

/// <summary>
/// Base class for all input actions (Command Pattern).
/// Encapsulates an input event that can be replayed on slave browsers.
/// </summary>
public abstract class InputAction
{
    public InputActionType ActionType { get; init; }
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
