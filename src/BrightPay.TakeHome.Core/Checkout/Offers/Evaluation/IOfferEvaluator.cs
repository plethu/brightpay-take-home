using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public interface IOfferEvaluator
{
    OfferType Type { get; }

    Type ConfigurationType { get; }

    AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition offer);
}

public interface IOfferEvaluator<TConfiguration> : IOfferEvaluator
    where TConfiguration : OfferConfiguration
{
    AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition<TConfiguration> offer);
}
