using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed record QuantityForFixedPriceConfiguration
{
    public QuantityForFixedPriceConfiguration(int quantity, Money fixedPrice)
    {
        if (quantity <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Offer quantity must be greater than one.");
        }

        if (!string.Equals(fixedPrice.Currency.Code, CheckoutMoney.CurrencyCode, StringComparison.Ordinal))
        {
            throw new ArgumentException("Offer prices must be in GBP.", nameof(fixedPrice));
        }

        if (fixedPrice.Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedPrice), fixedPrice, "Offer price cannot be negative.");
        }

        Quantity = quantity;
        FixedPrice = fixedPrice;
    }

    public int Quantity { get; }

    public Money FixedPrice { get; }
}
