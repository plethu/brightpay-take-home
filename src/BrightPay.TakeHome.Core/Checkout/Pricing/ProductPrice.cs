using NodaMoney;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Core.Checkout.Pricing;

public sealed record ProductPrice
{
    public ProductPrice(Sku sku, Money unitPrice)
    {
        CheckoutMoney.ThrowIfNotCheckoutCurrency(unitPrice, nameof(unitPrice), "Product prices must be in GBP.");
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice.Amount, nameof(unitPrice));

        Sku = sku;
        UnitPrice = unitPrice;
    }

    public Sku Sku { get; }

    public Money UnitPrice { get; }
}
