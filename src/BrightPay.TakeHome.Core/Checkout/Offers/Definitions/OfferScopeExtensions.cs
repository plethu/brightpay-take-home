namespace BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

public static class OfferScopeExtensions
{
    /// <summary>
    /// A line-attributed offer reduces the specific basket line it targets and is surfaced as a
    /// per-line applied offer. Every other scope is a whole-order promotion surfaced as a single
    /// basket adjustment, so one promotion is never split into misleading per-line discounts.
    /// New scopes must opt into a bucket here; the default arm throws rather than defaulting
    /// silently, so an unclassified scope fails loudly at the first projection.
    /// </summary>
    public static bool IsLineAttributed(this OfferScope scope) => scope switch
    {
        OfferScope.Line => true,
        OfferScope.Group or OfferScope.Basket => false,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unhandled offer scope."),
    };
}
