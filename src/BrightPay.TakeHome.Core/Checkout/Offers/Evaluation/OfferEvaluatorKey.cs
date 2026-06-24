using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public sealed record OfferEvaluatorKey
{
    public OfferEvaluatorKey(OfferType type, Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        Type = type;
        ConfigurationType = configurationType;
    }

    public OfferType Type { get; }

    public Type ConfigurationType { get; }
}
