using SyncBrowser.Core.Enums;

namespace SyncBrowser.Core.Models;

/// <summary>
/// Represents a mouse input action to be dispatched via CDP.
/// </summary>
public class MouseAction : InputAction
{
    public double X { get; init; }
    public double Y { get; init; }
    public MouseButton Button { get; init; } = MouseButton.None;
    public int ClickCount { get; init; }

    public static MouseAction Move(double x, double y) => new()
    {
        ActionType = InputActionType.MouseMove,
        X = x,
        Y = y,
        Button = MouseButton.None,
        ClickCount = 0
    };

    public static MouseAction Press(double x, double y, MouseButton button = MouseButton.Left) => new()
    {
        ActionType = InputActionType.MouseDown,
        X = x,
        Y = y,
        Button = button,
        ClickCount = 1
    };

    public static MouseAction Release(double x, double y, MouseButton button = MouseButton.Left) => new()
    {
        ActionType = InputActionType.MouseUp,
        X = x,
        Y = y,
        Button = button,
        ClickCount = 0
    };
}
