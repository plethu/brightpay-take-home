using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed record QuantityForFixedPriceConfiguration
{
    public QuantityForFixedPriceConfiguration(int quantity, Money fixedPrice)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantity, 1);
        CheckoutMoney.ThrowIfNotCheckoutCurrency(fixedPrice, nameof(fixedPrice), "Offer prices must be in GBP.");
        ArgumentOutOfRangeException.ThrowIfNegative(fixedPrice.Amount, nameof(fixedPrice));

        Quantity = quantity;
        FixedPrice = fixedPrice;
    }

    public int Quantity { get; }

    public Money FixedPrice { get; }
}
