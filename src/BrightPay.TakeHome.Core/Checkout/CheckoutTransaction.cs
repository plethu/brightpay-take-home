using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout;

public sealed class CheckoutTransaction
{
    private readonly Dictionary<Sku, ProductPrice> _prices;

    public CheckoutTransaction(IEnumerable<ProductPrice> prices, BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(basket);

        _prices = prices.ToDictionary(price => price.Sku);
        Basket = basket;
    }

    public BasketSnapshot Basket { get; }

    public Money Total => Basket.Lines.Aggregate(
        CheckoutMoney.Zero,
        (total, line) => total + (_prices[line.Sku].UnitPrice * line.Quantity));

    public CheckoutOperationResult Scan(string? skuText)
    {
        if (string.IsNullOrWhiteSpace(skuText))
        {
            return CheckoutOperationResult.Failure(Basket, CheckoutOperationError.EmptySku);
        }

        Sku? parsedSku = Sku.TryCreate(skuText);
        if (parsedSku is not { } sku || !_prices.ContainsKey(sku))
        {
            return CheckoutOperationResult.Failure(Basket, CheckoutOperationError.UnknownSku);
        }

        List<BasketLine> lines = [.. Basket.Lines];
        int existingIndex = lines.FindIndex(line => line.Sku == sku);
        if (existingIndex >= 0)
        {
            BasketLine existing = lines[existingIndex];
            lines[existingIndex] = new BasketLine(existing.Sku, existing.Quantity + 1);
        }
        else
        {
            lines.Add(new BasketLine(sku, quantity: 1));
        }

        return CheckoutOperationResult.Success(new BasketSnapshot(lines));
    }

    public CheckoutOperationResult RemoveLine(Sku sku)
    {
        return Basket.QuantityFor(sku) == 0
            ? CheckoutOperationResult.Failure(Basket, CheckoutOperationError.UnknownSku)
            : CheckoutOperationResult.Success(new BasketSnapshot([.. Basket.Lines.Where(line => line.Sku != sku)]));
    }

    public CheckoutOperationResult Clear()
    {
        return Basket.IsEmpty
            ? CheckoutOperationResult.Failure(Basket, CheckoutOperationError.EmptyBasket)
            : CheckoutOperationResult.Success(BasketSnapshot.Empty);
    }
}
