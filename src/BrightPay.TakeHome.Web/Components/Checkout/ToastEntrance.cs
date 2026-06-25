namespace BrightPay.TakeHome.Web.Components.Checkout;

/// <summary>
/// Which CSS animation a freshly keyed toast element plays when it (re)mounts.
/// </summary>
public enum ToastEntrance
{
    /// <summary>Slide in from hidden — a toast appearing on an empty corner.</summary>
    Enter,

    /// <summary>Pulse in place — a spaced add coalesced onto an already-visible toast.</summary>
    Pulse,

    /// <summary>No animation — text swapped in place (a bursty add fold, or a one-off replacing a visible toast).</summary>
    None,
}
