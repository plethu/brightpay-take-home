namespace BrightPay.TakeHome.Core.Checkout;

public sealed record BasketLine
{
    public BasketLine(Sku sku, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Basket line quantity must be positive.");
        }

        Sku = sku;
        Quantity = quantity;
    }

    public Sku Sku { get; }

    public int Quantity { get; }
}
