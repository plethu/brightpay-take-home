namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed record CheckoutTotalsView(
    string Subtotal,
    string Savings,
    string Total,
    int ItemCount,
    int LineCount,
    bool HasSavings,
    bool IsEmpty);
