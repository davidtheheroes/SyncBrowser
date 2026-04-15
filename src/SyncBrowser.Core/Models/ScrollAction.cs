using SyncBrowser.Core.Enums;

namespace SyncBrowser.Core.Models;

/// <summary>
/// Represents a scroll input action to be dispatched via CDP.
/// </summary>
public class ScrollAction : InputAction
{
    public double X { get; init; }
    public double Y { get; init; }
    public double DeltaX { get; init; }
    public double DeltaY { get; init; }

    public ScrollAction()
    {
        ActionType = InputActionType.Scroll;
    }

    public static ScrollAction Create(double x, double y, double deltaX, double deltaY) => new()
    {
        X = x,
        Y = y,
        DeltaX = deltaX,
        DeltaY = deltaY
    };
}
