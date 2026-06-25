using NodaMoney;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Projection;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Core.Checkout.Transactions;

/// <summary>
/// An in-flight checkout: applies basket operations (scan, increment, remove, clear) against a
/// fixed catalog snapshot and returns the resulting basket and offer-aware total. This is a
/// pricing/operation context, not a persisted financial transaction or a database transaction.
/// </summary>
public sealed class CheckoutTransaction
{
    private readonly Dictionary<Sku, ProductPrice> _prices;
    private readonly PricedBasketProjector _projector;

    public CheckoutTransaction(
        IEnumerable<ProductPrice> prices,
        IEnumerable<OfferDefinition> offers,
        IEnumerable<IOfferEvaluator> evaluators,
        BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(evaluators);
        ArgumentNullException.ThrowIfNull(basket);

        _prices = prices.ToDictionary(price => price.Sku);
        _projector = new PricedBasketProjector(_prices.Values, offers, evaluators);
        Basket = basket;
    }

    public BasketSnapshot Basket { get; }

    public Money Total => Project().Total;

    public PricedBasket Project() => _projector.Project(Basket);

    public CheckoutOperationResult Scan(string? skuText) => Add(skuText, quantity: 1);

    public CheckoutOperationResult Add(string? skuText, int quantity)
    {
        if (string.IsNullOrWhiteSpace(skuText))
        {
            return CheckoutOperationResult.Failure(Basket, CheckoutOperationError.EmptySku);
        }

        if (quantity < 1)
        {
            return CheckoutOperationResult.Failure(Basket, CheckoutOperationError.InvalidQuantity);
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
            lines[existingIndex] = new BasketLine(existing.Sku, existing.Quantity + quantity);
        }
        else
        {
            lines.Add(new BasketLine(sku, quantity));
        }

        return CheckoutOperationResult.Success(new BasketSnapshot(lines));
    }

    public CheckoutOperationResult Increment(Sku sku) => Add(sku.Value, quantity: 1);

    public CheckoutOperationResult Decrement(Sku sku)
    {
        List<BasketLine> lines = [.. Basket.Lines];
        int existingIndex = lines.FindIndex(line => line.Sku == sku);
        if (existingIndex < 0)
        {
            return CheckoutOperationResult.Failure(Basket, CheckoutOperationError.UnknownSku);
        }

        BasketLine existing = lines[existingIndex];
        if (existing.Quantity == 1)
        {
            lines.RemoveAt(existingIndex);
        }
        else
        {
            lines[existingIndex] = new BasketLine(existing.Sku, existing.Quantity - 1);
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

    public CheckoutOperationResult Charge() => Clear();
}
