namespace BrightPay.TakeHome.Web.Data.Checkout;

public sealed class CheckoutOfferEntity
{
    public required string Code { get; init; }

    public required string Sku { get; init; }

    // OfferType / OfferState persisted as their underlying int; the mapper validates the value.
    public int Type { get; init; }

    public int State { get; init; }

    public int ConfigurationVersion { get; init; }

    public required string ConfigurationJson { get; init; }

    public CheckoutProductEntity? Product { get; init; }
}
