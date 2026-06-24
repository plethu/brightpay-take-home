namespace BrightPay.TakeHome.Web.Data.Checkout;

public sealed class CheckoutOfferEntity
{
    public required string Code { get; init; }

    public required string Sku { get; init; }

    public int Type { get; init; }

    public int State { get; init; }

    public int Quantity { get; init; }

    public decimal FixedPriceAmount { get; init; }

    public CheckoutProductEntity? Product { get; init; }
}
