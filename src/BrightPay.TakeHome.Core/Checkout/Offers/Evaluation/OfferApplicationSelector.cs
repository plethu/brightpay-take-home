using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

internal sealed class OfferApplicationSelector
{
    private readonly OfferPlanningContext _context;

    public OfferApplicationSelector(OfferPlanningContext context)
    {
        _context = context;
    }

    public IReadOnlyList<OfferApplication> SelectBest(IReadOnlyList<OfferApplication> candidates)
    {
        List<OfferApplication> best = [];
        Search(0, candidates, [], new OfferLineConsumption());
        return best;

        void Search(
            int index,
            IReadOnlyList<OfferApplication> allCandidates,
            List<OfferApplication> selected,
            OfferLineConsumption consumed)
        {
            if (index >= allCandidates.Count)
            {
                if (OfferSelectionScore.From(selected).IsBetterThan(OfferSelectionScore.From(best)))
                {
                    best = [.. selected];
                }

                return;
            }

            Search(index + 1, allCandidates, selected, consumed);

            OfferApplication candidate = allCandidates[index];
            if (!CanSelect(candidate, consumed))
            {
                return;
            }

            selected.Add(candidate);
            Search(index + 1, allCandidates, selected, consumed.With(candidate));
            selected.RemoveAt(selected.Count - 1);
        }
    }

    private bool CanSelect(OfferApplication candidate, OfferLineConsumption consumed)
    {
        if (candidate.CombinationRule == OfferCombinationRule.Stackable)
        {
            return true;
        }

        foreach (OfferApplicationLineEffect effect in candidate.LineEffects)
        {
            if (consumed.QuantityFor(effect.LineReference) + effect.QuantityConsumed > LineQuantity(effect.LineReference))
            {
                return false;
            }
        }

        return true;
    }

    private int LineQuantity(string lineReference) =>
        _context.Basket.Lines
            .FirstOrDefault(line => string.Equals(line.Sku.Value, lineReference, StringComparison.Ordinal))
            ?.Quantity ?? 0;
}
