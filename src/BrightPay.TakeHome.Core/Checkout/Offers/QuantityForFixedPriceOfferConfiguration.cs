namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed record QuantityForFixedPriceOfferConfiguration(
    QuantityForFixedPriceConfiguration Value) : OfferConfiguration;
