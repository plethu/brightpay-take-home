namespace BrightPay.TakeHome.Core.Checkout.Offers;

public sealed class OfferEvaluatorRegistry
{
    private readonly Dictionary<OfferType, IOfferEvaluator> _evaluators;

    public OfferEvaluatorRegistry(IEnumerable<IOfferEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        _evaluators = evaluators.ToDictionary(evaluator => evaluator.Type);
    }

    public IOfferEvaluator? Resolve(OfferType type) =>
        _evaluators.TryGetValue(type, out IOfferEvaluator? evaluator) ? evaluator : null;
}
