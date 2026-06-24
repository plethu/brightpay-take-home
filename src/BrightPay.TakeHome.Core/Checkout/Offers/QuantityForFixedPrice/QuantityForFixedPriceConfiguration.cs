using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;

public sealed record QuantityForFixedPriceConfiguration : OfferConfiguration
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
