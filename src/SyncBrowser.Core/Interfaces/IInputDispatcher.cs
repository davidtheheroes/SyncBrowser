using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Interfaces;

/// <summary>
/// Strategy pattern: dispatches input actions to a target browser via CDP.
/// </summary>
public interface IInputDispatcher
{
    Task DispatchMouseAsync(object webView, MouseAction action);
    Task DispatchKeyboardAsync(object webView, KeyboardAction action);
    Task DispatchScrollAsync(object webView, ScrollAction action);
    Task DispatchAsync(object webView, InputAction action);
}
