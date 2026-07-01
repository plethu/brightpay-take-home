namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutCatalogItemView(
    string Sku,
    string Name,
    string UnitPrice,
    IReadOnlyList<string> OfferLabels)
{
    public CheckoutCatalogItemView(string sku, string name, string unitPrice, string? offerLabel)
        : this(sku, name, unitPrice, offerLabel is null ? [] : [offerLabel])
    {
    }

    public string? OfferLabel => OfferLabels.Count == 0 ? null : OfferLabels[0];
}
