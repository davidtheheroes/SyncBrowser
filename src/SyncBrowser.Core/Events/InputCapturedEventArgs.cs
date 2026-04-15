using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Events;

/// <summary>
/// Event data raised when the master browser captures an input action.
/// </summary>
public class InputCapturedEventArgs : EventArgs
{
    public InputAction Action { get; }
    public string SourceProfileId { get; }

    public InputCapturedEventArgs(InputAction action, string sourceProfileId)
    {
        Action = action;
        SourceProfileId = sourceProfileId;
    }
}
