namespace BrightPay.TakeHome.Web.Data.Checkout;

public sealed class CheckoutProductEntity
{
    public required string Sku { get; init; }

    // GBP minor units (pence); mapped to the checkout's Money convention. See CheckoutMoney.
    public decimal UnitPriceAmount { get; init; }

    public bool IsActive { get; init; }

    public ICollection<CheckoutOfferEntity> Offers { get; } = [];
}
