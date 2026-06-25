using System.Globalization;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Pricing;

/// <summary>
/// The checkout's money convention. Amounts are GBP <b>minor units</b> (pence): a
/// <see cref="Money"/> of 50 renders as £0.50. Build values with <see cref="FromPence"/> and
/// render them with <see cref="Format"/> only. NodaMoney's own formatting would treat the amount
/// as whole pounds (£50.00), so it must never be called directly on these values.
/// </summary>
public static class CheckoutMoney
{
    public const string CurrencyCode = "GBP";

    // Single source of truth for money display culture. Mirrored by Program.cs supportedCultures.
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    public static Money FromPence(decimal pence) => new(pence, CurrencyCode);

    public static Money Zero => FromPence(0m);

    public static string Format(Money money) =>
        (money.Amount / 100m).ToString("C", DisplayCulture);

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
