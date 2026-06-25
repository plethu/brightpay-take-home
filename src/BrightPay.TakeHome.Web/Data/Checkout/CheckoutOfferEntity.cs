namespace BrightPay.TakeHome.Web.Data.Checkout;

// Persistence is currently shaped for the one offer type (QuantityForFixedPrice): Quantity and
// FixedPriceAmount are its parameters. Adding an offer with a different shape needs a config
// strategy rather than more nullable columns — table-per-hierarchy on Type, or a single JSON
// Configuration column deserialized per Type. Keep that in mind before extending the offer engine.
public sealed class CheckoutOfferEntity
{
    public required string Code { get; init; }

    public required string Sku { get; init; }

    // OfferType / OfferState persisted as their underlying int; the mapper validates the value.
    public int Type { get; init; }

    public int State { get; init; }

    public int Quantity { get; init; }

    // GBP minor units (pence). See CheckoutMoney.
    public decimal FixedPriceAmount { get; init; }

    public CheckoutProductEntity? Product { get; init; }
}
