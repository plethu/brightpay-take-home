using BrightPay.TakeHome.Core.Checkout.Identifiers;
using System.Diagnostics.CodeAnalysis;

namespace BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

public sealed record OfferDefinition(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    OfferConfiguration Configuration,
    OfferScope Scope = OfferScope.Line,
    int Priority = 0,
    OfferCombinationRule CombinationRule = OfferCombinationRule.Exclusive)
{
    public bool IsActive => State == OfferState.Active;

    public OfferDefinition<TConfiguration>? ToTyped<TConfiguration>()
        where TConfiguration : OfferConfiguration =>
        TryToTyped(out OfferDefinition<TConfiguration>? typed) ? typed : null;

    public bool TryToTyped<TConfiguration>([NotNullWhen(true)] out OfferDefinition<TConfiguration>? typed)
        where TConfiguration : OfferConfiguration
    {
        if (Configuration.GetType() == typeof(TConfiguration))
        {
            typed = new OfferDefinition<TConfiguration>(Code, Sku, Type, State, (TConfiguration)Configuration, Scope, Priority, CombinationRule);
            return true;
        }

        typed = null;
        return false;
    }
}

public sealed record OfferDefinition<TConfiguration>(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    TConfiguration Configuration,
    OfferScope Scope = OfferScope.Line,
    int Priority = 0,
    OfferCombinationRule CombinationRule = OfferCombinationRule.Exclusive)
    where TConfiguration : OfferConfiguration
{
    public bool IsActive => State == OfferState.Active;

    public OfferDefinition ToUntyped() => new(Code, Sku, Type, State, Configuration, Scope, Priority, CombinationRule);
}
