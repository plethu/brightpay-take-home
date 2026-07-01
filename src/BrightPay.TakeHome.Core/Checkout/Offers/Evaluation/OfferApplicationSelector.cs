using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

internal sealed class OfferApplicationSelector
{
    private readonly IReadOnlyDictionary<string, int> _lineQuantities;

    public OfferApplicationSelector(OfferPlanningContext context)
    {
        _lineQuantities = BuildLineQuantities(context.Basket);
    }

    public IReadOnlyList<OfferApplication> SelectBest(IReadOnlyList<OfferApplication> candidates)
    {
        // Exact-optimal subset search. Suffix sums of the (always positive) candidate savings give
        // an upper bound on the savings still reachable from any point, so a branch is abandoned once
        // even taking every remaining candidate cannot beat the best savings found so far. This keeps
        // the search tractable as the active-offer count grows without changing the chosen result.
        decimal[] reachableSavings = SuffixSavings(candidates);

        List<OfferApplication> best = [];
        decimal bestSavings = 0m;
        Search(0, [], new OfferLineConsumption());
        return best;

        void Search(int index, List<OfferApplication> selected, OfferLineConsumption consumed)
        {
            if (index >= candidates.Count)
            {
                OfferSelectionScore score = OfferSelectionScore.From(selected);
                if (score.IsBetterThan(OfferSelectionScore.From(best)))
                {
                    best = [.. selected];
                    bestSavings = score.Savings;
                }

                return;
            }

            decimal selectedSavings = selected.Sum(application => application.Saving.Amount);
            // Strictly less: equal savings can still improve the priority/code tie-breaks, so never prune ties.
            if (selectedSavings + reachableSavings[index] < bestSavings)
            {
                return;
            }

            Search(index + 1, selected, consumed);

            OfferApplication candidate = candidates[index];
            if (!CanSelect(candidate, consumed))
            {
                return;
            }

            selected.Add(candidate);
            Search(index + 1, selected, consumed.With(candidate));
            selected.RemoveAt(selected.Count - 1);
        }
    }

    private bool CanSelect(OfferApplication candidate, OfferLineConsumption consumed)
    {
        // Stackable offers apply on top of others and deliberately reserve no line quantity, so an
        // exclusive offer and any number of stackable offers can all claim the same units. Exclusive
        // offers compete for a finite quantity and are gated by what earlier selections consumed.
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

    private int LineQuantity(string lineReference) => _lineQuantities.GetValueOrDefault(lineReference);

    private static decimal[] SuffixSavings(IReadOnlyList<OfferApplication> candidates)
    {
        decimal[] suffix = new decimal[candidates.Count + 1];
        for (int index = candidates.Count - 1; index >= 0; index--)
        {
            suffix[index] = suffix[index + 1] + candidates[index].Saving.Amount;
        }

        return suffix;
    }

    private static Dictionary<string, int> BuildLineQuantities(BasketSnapshot basket)
    {
        Dictionary<string, int> quantities = new(StringComparer.Ordinal);
        foreach (BasketLine line in basket.Lines)
        {
            // The basket holds one line per SKU today; summing keeps available quantity correct if a
            // future model ever emits multiple lines for the same line reference.
            quantities[line.Sku.Value] = quantities.GetValueOrDefault(line.Sku.Value) + line.Quantity;
        }

        return quantities;
    }
}
