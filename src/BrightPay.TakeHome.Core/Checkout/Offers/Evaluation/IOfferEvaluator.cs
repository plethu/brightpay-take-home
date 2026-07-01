using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Pricing;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public interface IOfferEvaluator
{
    OfferType Type { get; }

    Type ConfigurationType { get; }

    // Prices are supplied per evaluation rather than captured in the evaluator, so evaluators are
    // stateless and can be registered once in DI (the composition root owns the evaluator set).
    IReadOnlyList<OfferApplication> Evaluate(
        BasketSnapshot basket,
        OfferDefinition offer,
        IReadOnlyDictionary<Sku, ProductPrice> prices);
}

public interface IOfferEvaluator<TConfiguration> : IOfferEvaluator
    where TConfiguration : OfferConfiguration
{
    IReadOnlyList<OfferApplication> Evaluate(
        BasketSnapshot basket,
        OfferDefinition<TConfiguration> offer,
        IReadOnlyDictionary<Sku, ProductPrice> prices);
}
