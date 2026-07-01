using BrightPay.TakeHome.Core.Checkout.Identifiers;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed record PricedBasketLine(
    Sku Sku,
    int Quantity,
    Money UnitPrice,
    Money Subtotal,
    Money Savings,
    Money Total,
    IReadOnlyList<AppliedOfferSummary>? AppliedOffers = null)
{
    public IReadOnlyList<AppliedOfferSummary> AppliedOffers { get; init; } = AppliedOffers ?? [];

    public AppliedOfferSummary? AppliedOffer => AppliedOffers.Count == 0 ? null : AppliedOffers[0];
}
