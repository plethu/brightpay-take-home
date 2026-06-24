namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed record OfferDefinition(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    OfferConfiguration Configuration)
{
    public bool IsActive => State == OfferState.Active;
}
