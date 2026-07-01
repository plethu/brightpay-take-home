using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed record PricedBasket(
    IReadOnlyList<PricedBasketLine> Lines,
    Money Subtotal,
    Money Savings,
    Money Total,
    IReadOnlyList<PricedBasketAdjustment>? Adjustments = null)
{
    public IReadOnlyList<PricedBasketAdjustment> Adjustments { get; init; } = Adjustments ?? [];

    public int ItemCount => Lines.Sum(line => line.Quantity);

    public int LineCount => Lines.Count;
}
