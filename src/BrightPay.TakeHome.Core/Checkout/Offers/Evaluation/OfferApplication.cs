using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record OfferApplication(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferScope Scope,
    int Priority,
    OfferCombinationRule CombinationRule,
    int Applications,
    Money Saving,
    IReadOnlyList<OfferApplicationLineEffect> LineEffects);
