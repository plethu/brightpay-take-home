using NodaMoney;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed record AppliedOffer(string Code, Sku Sku, int Applications, Money Saving);
