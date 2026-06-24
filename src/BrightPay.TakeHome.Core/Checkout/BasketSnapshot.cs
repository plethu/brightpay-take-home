using System.Collections.Immutable;

namespace BrightPay.TakeHome.Core.Checkout;

public sealed record BasketSnapshot
{
    public static BasketSnapshot Empty { get; } = new([]);

    public BasketSnapshot(IReadOnlyCollection<BasketLine> lines)
    {
        Lines = lines
            .OrderBy(line => line.Sku.Value, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public IReadOnlyList<BasketLine> Lines { get; }

    public bool IsEmpty => Lines.Count == 0;

    public int QuantityFor(Sku sku) => Lines.FirstOrDefault(line => line.Sku == sku)?.Quantity ?? 0;
}
