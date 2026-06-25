using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Web.Components.Checkout;

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
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
        _batch.Clear();
        _dismiss?.Cancel();
        _dismiss?.Dispose();
        Toast = message;
        ToastLeaving = false;

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

        TimeSpan elapsed = _timeProvider.GetUtcNow() - _lastToastAt;
        bool visible = Toast is not null && !ToastLeaving;
        TimeSpan delay = visible
            ? elapsed >= ToastDebounceDelay ? TimeSpan.Zero : ToastDebounceDelay - elapsed
            : ToastDebounceDelay;

        StartDebounce(delay);
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

    private void StartDebounce(TimeSpan delay)
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        CancellationTokenSource debounce = new();
        _debounce = debounce;
        _ = FlushDebounceAsync(debounce, delay);
    }

    private async Task FlushDebounceAsync(CancellationTokenSource debounce, TimeSpan delay)
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

        _debounce = null;
        if (_dismiss is { } prevDismiss)
        {
            await prevDismiss.CancelAsync().ConfigureAwait(true);
        }

        _dismiss?.Dispose();
        Toast = BuildBatchMessage();
        ToastLeaving = false;
        _lastToastAt = _timeProvider.GetUtcNow();
        _batch.Clear();

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
            await _onStateChanged().ConfigureAwait(true);
        }
    }

    private sealed record ToastBatchLine(string Name, int Quantity);
}
