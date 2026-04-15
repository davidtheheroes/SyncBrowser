using SyncBrowser.Core.Enums;

namespace SyncBrowser.Core.Models;

/// <summary>
/// Represents a keyboard input action to be dispatched via CDP.
/// </summary>
public class KeyboardAction : InputAction
{
    public string Key { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int WindowsVirtualKeyCode { get; init; }
    public int Modifiers { get; init; }
    public string? Text { get; init; }

    public static KeyboardAction KeyDown(string key, string code, int vk, int modifiers = 0) => new()
    {
        ActionType = InputActionType.KeyDown,
        Key = key,
        Code = code,
        WindowsVirtualKeyCode = vk,
        Modifiers = modifiers
    };

    public static KeyboardAction KeyUp(string key, string code, int vk, int modifiers = 0) => new()
    {
        ActionType = InputActionType.KeyUp,
        Key = key,
        Code = code,
        WindowsVirtualKeyCode = vk,
        Modifiers = modifiers
    };

    public static KeyboardAction Char(string text) => new()
    {
        ActionType = InputActionType.KeyChar,
        Text = text
    };
}
