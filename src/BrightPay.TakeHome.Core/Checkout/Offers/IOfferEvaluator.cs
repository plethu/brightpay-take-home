using BrightPay.TakeHome.Core.Checkout.Basket;

namespace BrightPay.TakeHome.Core.Checkout.Offers;

public interface IOfferEvaluator
{
    OfferType Type { get; }

    AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition offer);
}
