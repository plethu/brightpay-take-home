namespace BrightPay.TakeHome.Web.Features.Checkout.Notifications;

/// <summary>
/// Which CSS animation a freshly keyed toast element plays when it mounts.
/// </summary>
public enum ToastEntrance
{
    /// <summary>Slide in from hidden when a toast appears in an empty corner.</summary>
    Enter,

    /// <summary>Pulse in place when a spaced add coalesces onto an already-visible toast.</summary>
    Pulse,

    /// <summary>Swap text in place without a new entrance animation.</summary>
    None,
}
