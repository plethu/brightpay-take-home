using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using NodaMoney;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record OfferApplicationPlan(IReadOnlyList<OfferApplication> Applications)
{
    public Money TotalSavings => Applications.Aggregate(CheckoutMoney.Zero, (sum, application) => sum + application.Saving);

    public Money SavingsForLine(string lineReference) =>
        Applications
            .SelectMany(application => application.LineEffects)
            .Where(effect => string.Equals(effect.LineReference, lineReference, StringComparison.Ordinal))
            .Aggregate(CheckoutMoney.Zero, (sum, effect) => sum + (effect.AllocatedSavings ?? CheckoutMoney.Zero));

    public IReadOnlyList<OfferApplication> ApplicationsForLine(string lineReference) =>
        [
            .. Applications
                .Where(application => application.LineEffects.Any(effect =>
                    string.Equals(effect.LineReference, lineReference, StringComparison.Ordinal)))
                .OrderBy(application => application.Code, StringComparer.Ordinal),
        ];

    public IReadOnlyList<OfferApplication> BasketApplications =>
        [.. Applications.Where(application => application.Scope == OfferScope.Basket)];
}
