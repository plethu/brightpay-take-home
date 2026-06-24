using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;

public sealed class QuantityForFixedPriceEvaluator : OfferEvaluator<QuantityForFixedPriceConfiguration>
{
    private readonly Dictionary<Sku, ProductPrice> _prices;

    public override OfferType Type => OfferType.QuantityForFixedPrice;

    public QuantityForFixedPriceEvaluator(IEnumerable<ProductPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);

        _prices = prices.ToDictionary(price => price.Sku);
    }

    public override AppliedOffer? Evaluate(
        BasketSnapshot basket,
        OfferDefinition<QuantityForFixedPriceConfiguration> offer)
    {
        ArgumentNullException.ThrowIfNull(basket);
        ArgumentNullException.ThrowIfNull(offer);

        if (!offer.IsActive || !_prices.TryGetValue(offer.Sku, out ProductPrice? price))
        {
            return null;
        }

        int quantity = basket.QuantityFor(offer.Sku);
        int applications = quantity / offer.Configuration.Quantity;
        if (applications == 0)
        {
            return null;
        }

        Money undiscounted = price.UnitPrice * offer.Configuration.Quantity * applications;
        Money discounted = offer.Configuration.FixedPrice * applications;

        return new AppliedOffer(offer.Code, offer.Sku, applications, undiscounted - discounted);
    }
}
