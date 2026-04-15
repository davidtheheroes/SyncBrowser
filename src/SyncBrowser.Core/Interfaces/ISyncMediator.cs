using SyncBrowser.Core.Events;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Interfaces;

/// <summary>
/// Mediator pattern: coordinates input synchronization between a master
/// browser and multiple slave browsers.
/// </summary>
public interface ISyncMediator : IDisposable
{
    /// <summary>Fired when sync status changes (started/stopped/error).</summary>
    event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    /// <summary>Whether sync is currently active.</summary>
    bool IsSyncing { get; }

    /// <summary>The profile ID of the current master browser.</summary>
    string? MasterProfileId { get; }

    /// <summary>Register a browser as the master (input source).</summary>
    void SetMaster(string profileId, object webView);

    /// <summary>Register a browser as a slave (input target).</summary>
    void AddSlave(string profileId, object webView);

    /// <summary>Remove a slave browser.</summary>
    void RemoveSlave(string profileId);

    /// <summary>Clear all registrations.</summary>
    void ClearAll();

    /// <summary>Start dispatching captured input to all slaves.</summary>
    void StartSync();

    /// <summary>Stop dispatching.</summary>
    void StopSync();

    /// <summary>Called by the master to broadcast an input action.</summary>
    Task OnInputCapturedAsync(InputAction action);
}
