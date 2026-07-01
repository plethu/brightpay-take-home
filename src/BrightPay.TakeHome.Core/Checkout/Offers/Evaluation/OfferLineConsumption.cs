using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

internal sealed class OfferLineConsumption
{
    private readonly Dictionary<string, int> _quantities;

    public OfferLineConsumption()
        : this(new Dictionary<string, int>(StringComparer.Ordinal))
    {
    }

    private OfferLineConsumption(Dictionary<string, int> quantities)
    {
        _quantities = quantities;
    }

    public int QuantityFor(string lineReference) => _quantities.GetValueOrDefault(lineReference);

    public OfferLineConsumption With(OfferApplication application)
    {
        Dictionary<string, int> copy = new(_quantities, StringComparer.Ordinal);
        if (application.CombinationRule == OfferCombinationRule.Stackable)
        {
            return new OfferLineConsumption(copy);
        }

        foreach (OfferApplicationLineEffect effect in application.LineEffects)
        {
            copy[effect.LineReference] = copy.GetValueOrDefault(effect.LineReference) + effect.QuantityConsumed;
        }

        return new OfferLineConsumption(copy);
    }
}
