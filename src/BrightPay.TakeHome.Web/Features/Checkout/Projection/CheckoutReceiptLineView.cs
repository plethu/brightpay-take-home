namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutReceiptLineView(
    string Sku,
    string Name,
    int Quantity,
    string Total,
    string? PreOfferTotal,
    string? OfferLabel);
