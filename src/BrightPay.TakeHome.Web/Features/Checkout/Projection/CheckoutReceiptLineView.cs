namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutReceiptLineView(
    string Sku,
    string Name,
    int Quantity,
    string Total,
    string? PreOfferTotal,
    IReadOnlyList<string> OfferLabels)
{
    public CheckoutReceiptLineView(
        string sku,
        string name,
        int quantity,
        string total,
        string? preOfferTotal,
        string? offerLabel)
        : this(sku, name, quantity, total, preOfferTotal, offerLabel is null ? [] : [offerLabel])
    {
    }

    public string? OfferLabel => OfferLabels.Count == 0 ? null : OfferLabels[0];
}
