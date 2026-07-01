using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed class BestCustomerValueOfferApplicationPlanner : IOfferApplicationPlanner
{
    public OfferApplicationPlan Plan(OfferPlanningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<OfferApplication> candidates = CreateCandidates(context);
        IReadOnlyList<OfferApplication> selected = new OfferApplicationSelector(context).SelectBest(candidates);

        return new OfferApplicationPlan(
            [
                .. selected
                    .OrderBy(application => application.Scope)
                    .ThenBy(application => application.Code, StringComparer.Ordinal),
            ]);
    }

    private static IReadOnlyList<OfferApplication> CreateCandidates(OfferPlanningContext context)
    {
        List<OfferApplication> candidates = [];
        foreach (OfferDefinition offer in context.ActiveOffers)
        {
            IOfferEvaluator evaluator = context.Evaluators.Resolve(offer)
                ?? throw new InvalidOperationException($"No evaluator is registered for active offer '{offer.Code}' ({offer.Type}).");
            candidates.AddRange(evaluator.Evaluate(context.Basket, offer, context.Prices));
        }

        return
        [
            .. candidates
                .Where(candidate => candidate.Saving.Amount > 0m)
                .OrderBy(candidate => candidate.Code, StringComparer.Ordinal),
        ];
    }
}
