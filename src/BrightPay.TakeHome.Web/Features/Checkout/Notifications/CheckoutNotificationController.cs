using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Web.Features.Checkout.Notifications;

internal sealed class CheckoutNotificationController : IDisposable
{
    private static readonly TimeSpan ToastDebounceDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan ToastDismissDelay = TimeSpan.FromMilliseconds(1800);

    // These two mirror CSS exit-animation durations so the element is removed exactly as it finishes
    // animating. Source of truth is app.css: ToastExitDelay → --motion-toast-exit (160ms),
    // ErrorExitDelay → --motion-error-exit (180ms). Keep them in sync.
    public static readonly TimeSpan ToastExitDelay = TimeSpan.FromMilliseconds(160);
    public static readonly TimeSpan ErrorExitDelay = TimeSpan.FromMilliseconds(180);

    private readonly Func<Task> _onStateChanged;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly Func<string, string?> _findItemName;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, ToastBatchLine> _batch =
        new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _debounce;
    private CancellationTokenSource? _dismiss;
    private CancellationTokenSource? _errorClear;
    private DateTimeOffset _lastToastAt = DateTimeOffset.MinValue;

    public string? Toast { get; internal set; }
    public bool ToastLeaving { get; private set; }

    // Bumped every time a visible toast (re)renders. The markup keys the toast element on this, so
    // Blazor remounts the node and the browser replays ToastEntrance's CSS animation — the no-JS way
    // to retrigger an animation in place (the old .razor.js pulseToast call did this via JS interop).
    public int ToastGeneration { get; private set; }
    public ToastEntrance ToastEntrance { get; private set; } = ToastEntrance.Enter;
    public string? ErrorMessage { get; internal set; }
    public bool ErrorIsInvalid { get; private set; }
    public bool ErrorLeaving { get; private set; }

    public CheckoutNotificationController(
        Func<Task> onStateChanged,
        IStringLocalizer<SharedResource> localizer,
        Func<string, string?> findItemName,
        TimeProvider timeProvider)
    {
        _onStateChanged = onStateChanged;
        _localizer = localizer;
        _findItemName = findItemName;
        _timeProvider = timeProvider;
    }

    public void ShowSuccessToast(string message)
    {
        bool wasVisible = Toast is not null && !ToastLeaving;
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
        _batch.Clear();
        _dismiss?.Cancel();
        _dismiss?.Dispose();
        Toast = message;
        ToastLeaving = false;

        // One-off messages (charge/clear) never pulse: slide in when fresh, swap in place when one is
        // already up. Mirrors the mockup's showToast(pulse: false).
        SetEntrance(wasVisible ? ToastEntrance.None : ToastEntrance.Enter);

        CancellationTokenSource dismiss = new();
        _dismiss = dismiss;
        _ = HideToastAfterDelayAsync(dismiss);
    }

    public void QueueAddedToast(string skuText, int quantity)
    {
        if (string.IsNullOrWhiteSpace(skuText))
        {
            return;
        }

        string name = _findItemName(skuText) ?? skuText;
        _batch[skuText] = _batch.TryGetValue(skuText, out ToastBatchLine? existing)
            ? (existing with { Quantity = existing.Quantity + quantity })
            : new ToastBatchLine(name, quantity);

        // Mirrors the mockup's notifyAdd: a spaced add onto a visible toast flushes immediately and
        // pulses; a bursty add (within the debounce window) folds in silently on a trailing flush; an
        // add onto no toast waits out the debounce and then slides in.
        TimeSpan elapsed = _timeProvider.GetUtcNow() - _lastToastAt;
        bool visible = Toast is not null && !ToastLeaving;
        bool pulse = visible && elapsed >= ToastDebounceDelay;
        TimeSpan delay = visible
            ? elapsed >= ToastDebounceDelay ? TimeSpan.Zero : ToastDebounceDelay - elapsed
            : ToastDebounceDelay;

        StartDebounce(delay, pulse);
    }

    public async Task DismissToastAsync()
    {
        CancellationTokenSource dismiss = new();
        if (_dismiss is { } prevDismiss)
        {
            await prevDismiss.CancelAsync().ConfigureAwait(true);
        }

        _dismiss?.Dispose();
        _dismiss = dismiss;
        await HideToastAsync(dismiss).ConfigureAwait(true);
    }

    public async Task ClearErrorAsync()
    {
        ErrorIsInvalid = false;
        if (_errorClear is { } prevClear)
        {
            await prevClear.CancelAsync().ConfigureAwait(true);
        }

        _errorClear?.Dispose();

        if (ErrorMessage is null)
        {
            ErrorLeaving = false;
            return;
        }

        ErrorLeaving = true;
        CancellationTokenSource clear = new();
        _errorClear = clear;

        try
        {
            await Task.Delay(ErrorExitDelay, _timeProvider, clear.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ReferenceEquals(_errorClear, clear))
        {
            ErrorMessage = null;
            ErrorLeaving = false;
            _errorClear = null;
            await _onStateChanged().ConfigureAwait(true);
        }
    }

    public void ApplyFailure(string? errorMessage)
    {
        CancelToast();
        ErrorMessage = errorMessage;
        ErrorIsInvalid = true;
        ErrorLeaving = false;
        Toast = null;
        ToastLeaving = false;
    }

    public void CancelToast()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
        _dismiss?.Cancel();
        _dismiss?.Dispose();
        _dismiss = null;
        _batch.Clear();
    }

    public void Dispose()
    {
        CancelToast();
        _errorClear?.Cancel();
        _errorClear?.Dispose();
    }

    private void SetEntrance(ToastEntrance entrance)
    {
        ToastEntrance = entrance;
        ToastGeneration++;
    }

    private void StartDebounce(TimeSpan delay, bool pulse)
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        CancellationTokenSource debounce = new();
        _debounce = debounce;
        _ = FlushDebounceAsync(debounce, delay, pulse);
    }

    private async Task FlushDebounceAsync(CancellationTokenSource debounce, TimeSpan delay, bool pulse)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, _timeProvider, debounce.Token).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!ReferenceEquals(_debounce, debounce) || _batch.Count == 0)
        {
            return;
        }

        bool wasVisible = Toast is not null && !ToastLeaving;
        _debounce = null;
        if (_dismiss is { } prevDismiss)
        {
            await prevDismiss.CancelAsync().ConfigureAwait(true);
        }

        _dismiss?.Dispose();
        // The batch is NOT cleared here: it accumulates across every flush for as long as the toast
        // stays up, so successive adds grow the same "Added N × …" count in place (cleared only when
        // the toast finally hides, in HideToastAsync). Clearing per flush would restart the count at 1
        // on each click — a new toast per add instead of an incrementing one.
        Toast = BuildBatchMessage();
        ToastLeaving = false;
        SetEntrance(wasVisible ? (pulse ? ToastEntrance.Pulse : ToastEntrance.None) : ToastEntrance.Enter);
        _lastToastAt = _timeProvider.GetUtcNow();

        CancellationTokenSource dismiss = new();
        _dismiss = dismiss;
        _ = HideToastAfterDelayAsync(dismiss);

        await _onStateChanged().ConfigureAwait(true);
    }

    private string BuildBatchMessage()
    {
        if (_batch.Count == 1)
        {
            ToastBatchLine line = _batch.Values.First();
            return _localizer["CheckoutToast_AddedQuantity", line.Quantity, line.Name].Value;
        }

        int total = _batch.Values.Sum(l => l.Quantity);
        return _localizer["CheckoutToast_AddedItems", total].Value;
    }

    private async Task HideToastAfterDelayAsync(CancellationTokenSource dismiss)
    {
        try
        {
            await Task.Delay(ToastDismissDelay, _timeProvider, dismiss.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await HideToastAsync(dismiss).ConfigureAwait(true);
    }

    private async Task HideToastAsync(CancellationTokenSource dismiss)
    {
        if (!ReferenceEquals(_dismiss, dismiss) || Toast is null)
        {
            return;
        }

        ToastLeaving = true;
        await _onStateChanged().ConfigureAwait(true);

        try
        {
            await Task.Delay(ToastExitDelay, _timeProvider, dismiss.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ReferenceEquals(_dismiss, dismiss))
        {
            Toast = null;
            ToastLeaving = false;
            _dismiss = null;
            // End of this toast's life: drop the accumulated count so the next add starts a fresh toast.
            _batch.Clear();
            await _onStateChanged().ConfigureAwait(true);
        }
    }

    private sealed record ToastBatchLine(string Name, int Quantity);
}
