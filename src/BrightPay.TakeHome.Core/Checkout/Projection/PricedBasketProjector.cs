using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed class PricedBasketProjector
{
    private readonly Dictionary<Sku, ProductPrice> _prices;
    private readonly IReadOnlyList<OfferDefinition> _offers;
    private readonly OfferEvaluatorRegistry _offerEvaluators;

    public PricedBasketProjector(IEnumerable<ProductPrice> prices, IEnumerable<OfferDefinition> offers)
        : this(prices, offers, CreateDefaultEvaluators)
    {
    }

    public PricedBasketProjector(
        IEnumerable<ProductPrice> prices,
        IEnumerable<OfferDefinition> offers,
        Func<IReadOnlyCollection<ProductPrice>, IEnumerable<IOfferEvaluator>> evaluatorFactory)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(evaluatorFactory);

        ProductPrice[] productPrices = [.. prices];
        _prices = productPrices.ToDictionary(price => price.Sku);
        _offers =
        [
            .. offers
                .Where(offer => offer.IsActive)
                .OrderBy(offer => offer.Sku.Value, StringComparer.Ordinal)
                .ThenBy(offer => offer.Code, StringComparer.Ordinal),
        ];
        _offerEvaluators = new OfferEvaluatorRegistry(evaluatorFactory(productPrices));
    }

    public PricedBasket Project(BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(basket);

        List<PricedBasketLine> lines = [];
        Money subtotal = CheckoutMoney.Zero;
        Money savings = CheckoutMoney.Zero;
        Money total = CheckoutMoney.Zero;

        foreach (BasketLine line in basket.Lines)
        {
            ProductPrice price = _prices[line.Sku];
            Money lineSubtotal = price.UnitPrice * line.Quantity;
            AppliedOfferSummary? appliedOffer = EvaluateBestOffer(basket, line.Sku);
            Money lineSavings = appliedOffer?.Savings ?? CheckoutMoney.Zero;

            Money lineTotal = lineSubtotal - lineSavings;
            lines.Add(new PricedBasketLine(line.Sku, line.Quantity, price.UnitPrice, lineSubtotal, lineSavings, lineTotal, appliedOffer));
            subtotal += lineSubtotal;
            savings += lineSavings;
            total += lineTotal;
        }

        return new PricedBasket(lines, subtotal, savings, total);
    }

    private AppliedOfferSummary? EvaluateBestOffer(BasketSnapshot basket, Sku sku)
    {
        AppliedOffer? bestOffer = null;

        foreach (OfferDefinition offer in _offers.Where(offer => offer.Sku == sku))
        {
            IOfferEvaluator evaluator = _offerEvaluators.Resolve(offer)
                ?? throw new InvalidOperationException($"No evaluator is registered for active offer '{offer.Code}' ({offer.Type}).");
            AppliedOffer? appliedOffer = evaluator.Evaluate(basket, offer);
            if (appliedOffer is not null && (bestOffer is null || appliedOffer.Saving > bestOffer.Saving))
            {
                bestOffer = appliedOffer;
            }
        }

        return bestOffer is null
            ? null
            : new AppliedOfferSummary(bestOffer.Code, bestOffer.Sku, bestOffer.Applications, bestOffer.Saving);
    }

    private static IEnumerable<IOfferEvaluator> CreateDefaultEvaluators(IReadOnlyCollection<ProductPrice> prices) =>
        [new QuantityForFixedPriceEvaluator(prices)];
}
