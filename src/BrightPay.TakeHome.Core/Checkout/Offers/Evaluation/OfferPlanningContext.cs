using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Pricing;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record OfferPlanningContext(
    BasketSnapshot Basket,
    IReadOnlyDictionary<Sku, ProductPrice> Prices,
    IReadOnlyList<OfferDefinition> ActiveOffers,
    OfferEvaluatorRegistry Evaluators);
