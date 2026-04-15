using System.Collections.Concurrent;
using System.Threading.Channels;
using SyncBrowser.Core.Enums;
using SyncBrowser.Core.Events;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Services;

/// <summary>
/// Mediator pattern implementation: coordinates input synchronization
/// between a master browser and multiple slave browsers.
///
/// Performance design:
///   - A bounded Channel acts as an async producer/consumer pipeline,
///     decoupling capture (UI thread) from dispatch (background).
///   - A single background loop drains the channel and coalesces
///     consecutive mouse-move / scroll events (latest-wins).
///   - All slaves receive each action in parallel via Task.WhenAll,
///     reducing per-event latency from O(n) to O(1).
/// </summary>
public class SyncMediator : ISyncMediator, IDisposable
{
    private readonly IInputDispatcher _dispatcher;
    private readonly ConcurrentDictionary<string, object> _slaves = new();
    private readonly Channel<InputAction> _channel;

    private string? _masterProfileId;
    private object? _masterWebView;
    private volatile bool _isSyncing;

    private CancellationTokenSource? _loopCts;
    private Task? _dispatchLoop;

    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    public bool IsSyncing => _isSyncing;
    public string? MasterProfileId => _masterProfileId;

    public SyncMediator(IInputDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _channel = Channel.CreateBounded<InputAction>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void SetMaster(string profileId, object webView)
    {
        _slaves.TryRemove(profileId, out _);

        _masterProfileId = profileId;
        _masterWebView = webView;

        RaiseSyncStatusChanged();
    }

    public void AddSlave(string profileId, object webView)
    {
        if (profileId == _masterProfileId) return;

        _slaves[profileId] = webView;
        RaiseSyncStatusChanged();
    }

    public void RemoveSlave(string profileId)
    {
        _slaves.TryRemove(profileId, out _);
        RaiseSyncStatusChanged();
    }

    public void ClearAll()
    {
        _masterProfileId = null;
        _masterWebView = null;
        _slaves.Clear();

        StopDispatchLoop();
        _isSyncing = false;
        RaiseSyncStatusChanged();
    }

    public void StartSync()
    {
        if (_masterWebView == null || _slaves.IsEmpty) return;

        _isSyncing = true;
        StartDispatchLoop();
        RaiseSyncStatusChanged();
    }

    public void StopSync()
    {
        _isSyncing = false;
        StopDispatchLoop();
        RaiseSyncStatusChanged();
    }

    public async Task OnInputCapturedAsync(InputAction action)
    {
        if (!_isSyncing || _slaves.IsEmpty) return;

        await _channel.Writer.WriteAsync(action);
    }

    private void StartDispatchLoop()
    {
        StopDispatchLoop();
        _loopCts = new CancellationTokenSource();
        // Run on the current SynchronizationContext (UI thread) so CDP calls
        // execute on the thread that owns the WebView2 instances.
        // The loop yields via await, keeping the UI responsive.
        _dispatchLoop = DispatchLoopAsync(_loopCts.Token);
    }

    private void StopDispatchLoop()
    {
        if (_loopCts == null) return;
        _loopCts.Cancel();
        // No blocking wait — the loop runs on the UI context and will exit
        // on its next await when it observes the cancelled token.
        _loopCts.Dispose();
        _loopCts = null;
        _dispatchLoop = null;

        DrainChannel();
    }

    /// <summary>
    /// Background loop: reads actions from the channel, coalesces
    /// mouse-move and scroll events, then dispatches to all slaves in parallel.
    /// </summary>
    private async Task DispatchLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var action in _channel.Reader.ReadAllAsync(ct))
            {
                var toDispatch = action;

                bool isCoalesceable = IsCoalesceable(action);
                if (isCoalesceable)
                {
                    while (_channel.Reader.TryRead(out var next))
                    {
                        if (IsSameCoalesceGroup(toDispatch, next))
                        {
                            toDispatch = Coalesce(toDispatch, next);
                        }
                        else
                        {
                            await DispatchToAllSlavesAsync(toDispatch);
                            toDispatch = next;
                        }
                    }
                }

                await DispatchToAllSlavesAsync(toDispatch);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task DispatchToAllSlavesAsync(InputAction action)
    {
        var slaves = _slaves.Values.ToArray();
        if (slaves.Length == 0) return;

        var tasks = new Task[slaves.Length];
        for (int i = 0; i < slaves.Length; i++)
        {
            tasks[i] = DispatchSafeAsync(slaves[i], action);
        }
        await Task.WhenAll(tasks);
    }

    private async Task DispatchSafeAsync(object slave, InputAction action)
    {
        try
        {
            await _dispatcher.DispatchAsync(slave, action);
        }
        catch
        {
            // Individual slave failure shouldn't stop others
        }
    }

    private static bool IsCoalesceable(InputAction action) =>
        action.ActionType is InputActionType.MouseMove or InputActionType.Scroll;

    private static bool IsSameCoalesceGroup(InputAction a, InputAction b) =>
        IsCoalesceable(b) && a.ActionType == b.ActionType;

    /// <summary>
    /// Mouse move: latest position wins (absolute coordinates).
    /// Scroll: accumulate deltas (relative movement) so no scroll distance is lost.
    /// </summary>
    private static InputAction Coalesce(InputAction current, InputAction next)
    {
        if (current is ScrollAction cur && next is ScrollAction nxt)
        {
            return ScrollAction.Create(
                nxt.X, nxt.Y,
                cur.DeltaX + nxt.DeltaX,
                cur.DeltaY + nxt.DeltaY);
        }

        return next;
    }

    private void DrainChannel()
    {
        while (_channel.Reader.TryRead(out _)) { }
    }

    private void RaiseSyncStatusChanged()
    {
        SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(
            _isSyncing, _slaves.Count));
    }

    public void Dispose()
    {
        StopDispatchLoop();
        _channel.Writer.TryComplete();
        GC.SuppressFinalize(this);
    }
}
