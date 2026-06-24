using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Core.Checkout.Basket;

public sealed record BasketLine
{
    public BasketLine(Sku sku, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        Sku = sku;
        Quantity = quantity;
    }

    public Sku Sku { get; }

    public int Quantity { get; }
}
