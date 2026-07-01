namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutViewModel(
    IReadOnlyList<CheckoutCatalogItemView> Catalog,
    IReadOnlyList<CheckoutReceiptLineView> Lines,
    CheckoutTotalsView Totals,
    IReadOnlyList<CheckoutAdjustmentView>? Adjustments = null)
{
    public IReadOnlyList<CheckoutAdjustmentView> Adjustments { get; init; } = Adjustments ?? [];
}
