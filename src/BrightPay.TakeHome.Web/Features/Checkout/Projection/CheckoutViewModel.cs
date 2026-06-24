namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutViewModel(
    IReadOnlyList<CheckoutCatalogItemView> Catalog,
    IReadOnlyList<CheckoutReceiptLineView> Lines,
    CheckoutTotalsView Totals);
