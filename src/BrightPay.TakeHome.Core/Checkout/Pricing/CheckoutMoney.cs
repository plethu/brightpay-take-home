using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Pricing;

public static class CheckoutMoney
{
    public const string CurrencyCode = "GBP";

    public static Money Pounds(decimal amount) => new(amount, CurrencyCode);

    public static Money Zero => Pounds(0m);

    public static void ThrowIfNotCheckoutCurrency(Money money, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paramName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!string.Equals(money.Currency.Code, CurrencyCode, StringComparison.Ordinal))
        {
            throw new ArgumentException(message, paramName);
        }
    }
}
