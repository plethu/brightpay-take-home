using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed class PricedBasketProjector
{
    private readonly Dictionary<Sku, ProductPrice> _prices;
    private readonly IReadOnlyList<OfferDefinition> _offers;
    private readonly OfferEvaluatorRegistry _offerEvaluators;
    private readonly IOfferApplicationPlanner _offerPlanner;

    public PricedBasketProjector(
        IEnumerable<ProductPrice> prices,
        IEnumerable<OfferDefinition> offers,
        IEnumerable<IOfferEvaluator> evaluators,
        IOfferApplicationPlanner? offerPlanner = null)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(evaluators);

        _prices = prices.ToDictionary(price => price.Sku);
        _offers =
        [
            .. offers
                .Where(offer => offer.IsActive)
                .OrderBy(offer => offer.Sku.Value, StringComparer.Ordinal)
                .ThenBy(offer => offer.Code, StringComparer.Ordinal),
        ];
        _offerEvaluators = new OfferEvaluatorRegistry(evaluators);
        _offerPlanner = offerPlanner ?? new BestCustomerValueOfferApplicationPlanner();
    }

    public PricedBasket Project(BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(basket);

        List<PricedBasketLine> lines = [];
        Money subtotal = CheckoutMoney.Zero;
        OfferApplicationPlan offerPlan = _offerPlanner.Plan(new OfferPlanningContext(basket, _prices, _offers, _offerEvaluators));

        foreach (BasketLine line in basket.Lines)
        {
            // A basket can outlive a catalog change and reference a SKU that is no longer priced.
            // Skip it rather than throwing KeyNotFoundException from the indexer.
            if (!_prices.TryGetValue(line.Sku, out ProductPrice? price))
            {
                continue;
            }

            Money lineSubtotal = price.UnitPrice * line.Quantity;
            string lineReference = LineReference(line);
            Money lineSavings = offerPlan.SavingsForLine(lineReference);
            AppliedOfferSummary? appliedOffer = offerPlan.ApplicationsForLine(lineReference)
                .Select(application => new AppliedOfferSummary(application.Code, application.Sku, application.Applications, application.Saving))
                .FirstOrDefault();

            Money lineTotal = lineSubtotal - lineSavings;
            lines.Add(new PricedBasketLine(line.Sku, line.Quantity, price.UnitPrice, lineSubtotal, lineSavings, lineTotal, appliedOffer));
            subtotal += lineSubtotal;
        }

        IReadOnlyList<PricedBasketAdjustment> adjustments =
        [
            .. offerPlan.BasketApplications.Select(application => new PricedBasketAdjustment(application.Code, application.Saving)),
        ];
        Money savings = offerPlan.TotalSavings;
        Money total = subtotal - savings;

        return new PricedBasket(lines, subtotal, savings, total, adjustments);
    }

    private static string LineReference(BasketLine line) => line.Sku.Value;
}
