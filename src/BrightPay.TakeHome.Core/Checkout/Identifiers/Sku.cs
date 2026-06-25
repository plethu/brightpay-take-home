using System.Globalization;
using Vogen;

namespace BrightPay.TakeHome.Core.Checkout.Identifiers;

[ValueObject<string>]
public readonly partial struct Sku
{
    public static Sku? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return From(value);
        }
        catch (ValueObjectValidationException)
        {
            return null;
        }
    }

    public static bool operator <(Sku left, Sku right)
    {
        return string.CompareOrdinal(left.Value, right.Value) < 0;
    }

    public static bool operator >(Sku left, Sku right)
    {
        return string.CompareOrdinal(left.Value, right.Value) > 0;
    }

    public static bool operator <=(Sku left, Sku right)
    {
        return string.CompareOrdinal(left.Value, right.Value) <= 0;
    }

    public static bool operator >=(Sku left, Sku right)
    {
        return string.CompareOrdinal(left.Value, right.Value) >= 0;
    }

    private static string NormalizeInput(string value) => value.Trim().ToUpper(CultureInfo.InvariantCulture);

    // The kata models SKUs as single letters A–Z; this constraint is mirrored by the DB column
    // length. Widen both together if real multi-character SKUs are ever needed.
    private static Validation Validate(string value) =>
        value.Length == 1 && value[0] is >= 'A' and <= 'Z'
            ? Validation.Ok
            : Validation.Invalid("SKU must be one uppercase letter from A to Z.");
}
