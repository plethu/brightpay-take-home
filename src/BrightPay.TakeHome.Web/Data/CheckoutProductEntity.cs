namespace BrightPay.TakeHome.Web.Data;

public sealed class CheckoutProductEntity
{
    public required string Sku { get; init; }

    public decimal UnitPriceAmount { get; init; }

    public bool IsActive { get; init; }

    public ICollection<CheckoutOfferEntity> Offers { get; } = [];
}
