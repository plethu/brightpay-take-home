using BrightPay.TakeHome.Core.Checkout.Identifiers;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record OfferApplicationLineEffect(
    string LineReference,
    Sku Sku,
    int QuantityConsumed,
    Money? AllocatedSavings = null);
