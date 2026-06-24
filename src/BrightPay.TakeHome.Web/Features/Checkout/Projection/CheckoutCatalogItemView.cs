namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutCatalogItemView(
    string Sku,
    string Name,
    string UnitPrice,
    string? OfferLabel);
