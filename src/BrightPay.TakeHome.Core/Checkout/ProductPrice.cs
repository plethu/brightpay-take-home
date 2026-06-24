using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout;

public sealed record ProductPrice
{
    public ProductPrice(Sku sku, Money unitPrice)
    {
        if (!string.Equals(unitPrice.Currency.Code, CheckoutMoney.CurrencyCode, StringComparison.Ordinal))
        {
            throw new ArgumentException("Product prices must be in GBP.", nameof(unitPrice));
        }

        if (unitPrice.Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Product prices cannot be negative.");
        }

        Sku = sku;
        UnitPrice = unitPrice;
    }

    public Sku Sku { get; }

    public Money UnitPrice { get; }
}
