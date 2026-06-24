using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout;

public static class CheckoutMoney
{
    public const string CurrencyCode = "GBP";

    public static Money Pounds(decimal amount) => new(amount, CurrencyCode);

    public static Money Zero => Pounds(0m);
}
