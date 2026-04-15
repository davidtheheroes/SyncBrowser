namespace SyncBrowser.Core.Events;

/// <summary>
/// Event data raised when the sync status changes.
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    public bool IsSyncing { get; }
    public int SlaveCount { get; }
    public string? ErrorMessage { get; }

    public SyncStatusChangedEventArgs(bool isSyncing, int slaveCount, string? errorMessage = null)
    {
        IsSyncing = isSyncing;
        SlaveCount = slaveCount;
        ErrorMessage = errorMessage;
    }
}
