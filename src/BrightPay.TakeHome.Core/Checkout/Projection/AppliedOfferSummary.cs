using BrightPay.TakeHome.Core.Checkout.Identifiers;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed record AppliedOfferSummary(string Code, Sku Sku, int Applications, Money Savings);
