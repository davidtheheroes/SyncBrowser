using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using SyncBrowser.Core.Enums;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Infrastructure.Dispatchers;

/// <summary>
/// Strategy pattern implementation: dispatches input actions to WebView2
/// instances using Chrome DevTools Protocol (CDP).
/// </summary>
public class CdpInputDispatcher : IInputDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task DispatchAsync(object webView, InputAction action)
    {
        switch (action)
        {
            case MouseAction mouse:
                await DispatchMouseAsync(webView, mouse);
                break;
            case KeyboardAction keyboard:
                await DispatchKeyboardAsync(webView, keyboard);
                break;
            case ScrollAction scroll:
                await DispatchScrollAsync(webView, scroll);
                break;
        }
    }

    public async Task DispatchMouseAsync(object webView, MouseAction action)
    {
        var coreWebView = GetCoreWebView2(webView);
        if (coreWebView == null) return;

        string type = action.ActionType switch
        {
            InputActionType.MouseMove => "mouseMoved",
            InputActionType.MouseDown => "mousePressed",
            InputActionType.MouseUp => "mouseReleased",
            _ => "mouseMoved"
        };

        string button = action.Button switch
        {
            MouseButton.Left => "left",
            MouseButton.Right => "right",
            MouseButton.Middle => "middle",
            _ => "none"
        };

        var param = new
        {
            type,
            x = action.X,
            y = action.Y,
            button,
            clickCount = action.ClickCount
        };

        await coreWebView.CallDevToolsProtocolMethodAsync(
            "Input.dispatchMouseEvent",
            JsonSerializer.Serialize(param, JsonOptions));
    }

    public async Task DispatchKeyboardAsync(object webView, KeyboardAction action)
    {
        var coreWebView = GetCoreWebView2(webView);
        if (coreWebView == null) return;

        string type = action.ActionType switch
        {
            InputActionType.KeyDown => "keyDown",
            InputActionType.KeyUp => "keyUp",
            InputActionType.KeyChar => "char",
            _ => "keyDown"
        };

        var param = new Dictionary<string, object>
        {
            ["type"] = type
        };

        if (action.ActionType == InputActionType.KeyChar && action.Text != null)
        {
            // char event only needs text
            param["text"] = action.Text;
        }
        else
        {
            param["key"] = action.Key;
            param["code"] = action.Code;
            param["windowsVirtualKeyCode"] = action.WindowsVirtualKeyCode;
            param["nativeVirtualKeyCode"] = action.WindowsVirtualKeyCode;

            if (action.Modifiers != 0)
            {
                param["modifiers"] = action.Modifiers;
            }

            // For keyDown of printable chars, include text for input insertion
            if (action.ActionType == InputActionType.KeyDown
                && action.Key.Length == 1
                && action.Modifiers == 0)
            {
                param["text"] = action.Key;
            }
        }

        await coreWebView.CallDevToolsProtocolMethodAsync(
            "Input.dispatchKeyEvent",
            JsonSerializer.Serialize(param, JsonOptions));
    }

    public async Task DispatchScrollAsync(object webView, ScrollAction action)
    {
        var coreWebView = GetCoreWebView2(webView);
        if (coreWebView == null) return;

        // CDP uses Input.dispatchMouseEvent with type "mouseWheel" for scroll
        var param = new
        {
            type = "mouseWheel",
            x = action.X,
            y = action.Y,
            deltaX = action.DeltaX,
            deltaY = action.DeltaY
        };

        await coreWebView.CallDevToolsProtocolMethodAsync(
            "Input.dispatchMouseEvent",
            JsonSerializer.Serialize(param, JsonOptions));
    }

    /// <summary>
    /// Extracts CoreWebView2 from the webView object.
    /// Supports both CoreWebView2 directly and WebView2 control.
    /// </summary>
    private static CoreWebView2? GetCoreWebView2(object webView)
    {
        return webView switch
        {
            CoreWebView2 core => core,
            _ => null
        };
    }
}
