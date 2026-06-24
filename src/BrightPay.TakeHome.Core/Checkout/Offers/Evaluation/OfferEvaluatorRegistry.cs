using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed class OfferEvaluatorRegistry
{
    private readonly Dictionary<OfferEvaluatorKey, IOfferEvaluator> _evaluators = [];

    public OfferEvaluatorRegistry(IEnumerable<IOfferEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        foreach (IOfferEvaluator evaluator in evaluators)
        {
            OfferEvaluatorKey key = new(evaluator.Type, evaluator.ConfigurationType);
            if (!_evaluators.TryAdd(key, evaluator))
            {
                throw new InvalidOperationException(
                    $"An offer evaluator is already registered for {key.Type} with {key.ConfigurationType.Name}.");
            }
        }
    }

    public IOfferEvaluator<TConfiguration>? Resolve<TConfiguration>(OfferType type)
        where TConfiguration : OfferConfiguration =>
        _evaluators.TryGetValue(new OfferEvaluatorKey(type, typeof(TConfiguration)), out IOfferEvaluator? evaluator)
            ? evaluator as IOfferEvaluator<TConfiguration>
            : null;

    public IOfferEvaluator? Resolve(OfferDefinition offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        OfferEvaluatorKey key = new(offer.Type, offer.Configuration.GetType());

        return _evaluators.TryGetValue(key, out IOfferEvaluator? evaluator) ? evaluator : null;
    }
}
