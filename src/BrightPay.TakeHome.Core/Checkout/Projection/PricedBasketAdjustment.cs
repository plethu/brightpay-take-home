using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Projection;

public sealed record PricedBasketAdjustment(string Code, Money Savings);
