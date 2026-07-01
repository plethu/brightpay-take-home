using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;

public sealed class QuantityForFixedPriceEvaluator : OfferEvaluator<QuantityForFixedPriceConfiguration>
{
    public override OfferType Type => OfferType.QuantityForFixedPrice;

    public override IReadOnlyList<OfferApplication> Evaluate(
        BasketSnapshot basket,
        OfferDefinition<QuantityForFixedPriceConfiguration> offer,
        IReadOnlyDictionary<Sku, ProductPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(basket);
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(prices);

        if (!offer.IsActive || !prices.TryGetValue(offer.Sku, out ProductPrice? price))
        {
            return [];
        }

        int quantity = basket.QuantityFor(offer.Sku);
        int applications = quantity / offer.Configuration.Quantity;
        if (applications == 0)
        {
            return [];
        }

        Money undiscounted = price.UnitPrice * offer.Configuration.Quantity * applications;
        Money discounted = offer.Configuration.FixedPrice * applications;

        Money saving = undiscounted - discounted;

        return
        [
            new OfferApplication(
                offer.Code,
                offer.Sku,
                offer.Type,
                offer.Scope,
                offer.Priority,
                offer.CombinationRule,
                applications,
                saving,
                [
                    new OfferApplicationLineEffect(
                        offer.Sku.Value,
                        offer.Sku,
                        offer.Configuration.Quantity * applications,
                        saving),
                ]),
        ];
    }
}
