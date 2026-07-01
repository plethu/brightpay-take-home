using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Pricing;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public abstract class OfferEvaluator<TConfiguration> : IOfferEvaluator<TConfiguration>
    where TConfiguration : OfferConfiguration
{
    public abstract OfferType Type { get; }

    public Type ConfigurationType => typeof(TConfiguration);

    public IReadOnlyList<OfferApplication> Evaluate(
        BasketSnapshot basket,
        OfferDefinition offer,
        IReadOnlyDictionary<Sku, ProductPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(offer);

        return offer.TryToTyped(out OfferDefinition<TConfiguration>? typedOffer)
            ? Evaluate(basket, typedOffer, prices)
            : throw new ArgumentException(
                $"Offer configuration must be {typeof(TConfiguration).Name}.",
                nameof(offer));
    }

    public abstract IReadOnlyList<OfferApplication> Evaluate(
        BasketSnapshot basket,
        OfferDefinition<TConfiguration> offer,
        IReadOnlyDictionary<Sku, ProductPrice> prices);
}
