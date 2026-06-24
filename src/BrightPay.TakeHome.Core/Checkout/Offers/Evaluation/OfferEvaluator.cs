using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public abstract class OfferEvaluator<TConfiguration> : IOfferEvaluator<TConfiguration>
    where TConfiguration : OfferConfiguration
{
    public abstract OfferType Type { get; }

    public Type ConfigurationType => typeof(TConfiguration);

    public AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        return offer.TryToTyped(out OfferDefinition<TConfiguration>? typedOffer)
            ? Evaluate(basket, typedOffer)
            : throw new ArgumentException(
                $"Offer configuration must be {typeof(TConfiguration).Name}.",
                nameof(offer));
    }

    public abstract AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition<TConfiguration> offer);
}
