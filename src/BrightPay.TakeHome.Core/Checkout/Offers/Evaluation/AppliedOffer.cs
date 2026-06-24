using BrightPay.TakeHome.Core.Checkout.Identifiers;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record AppliedOffer(string Code, Sku Sku, int Applications, Money Saving);
