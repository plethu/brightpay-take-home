using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class OfferDefinitionTests
{
    [Fact]
    public void QuantityForFixedPriceConfigurationKeepsTypedOfferShape()
    {
        OfferDefinition offer = new(
            "A-3-FOR-130",
            Sku.From("A"),
            OfferType.QuantityForFixedPrice,
            OfferState.Active,
            new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m)));

        offer.IsActive.Should().BeTrue();
        offer.Configuration.Should().BeOfType<QuantityForFixedPriceConfiguration>()
            .Which.FixedPrice.Amount.Should().Be(130m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void QuantityForFixedPriceRejectsNonOfferQuantities(int quantity)
    {
        Action action = () => GC.KeepAlive(new QuantityForFixedPriceConfiguration(quantity, CheckoutMoney.Pounds(130m)));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OfferDefinitionConvertsToTypedDefinitionWhenConfigurationMatches()
    {
        OfferDefinition offer = CreateQuantityForFixedPriceOffer();

        OfferDefinition<QuantityForFixedPriceConfiguration>? typed = offer.ToTyped<QuantityForFixedPriceConfiguration>();

        typed.Should().NotBeNull();
        typed!.Configuration.Quantity.Should().Be(3);
        typed.ToUntyped().Should().Be(offer);
    }

    [Fact]
    public void RegistryResolvesRegisteredEvaluatorByOfferTypeAndConfigurationType()
    {
        TestEvaluator evaluator = new();
        OfferEvaluatorRegistry registry = new([evaluator]);

        IOfferEvaluator<QuantityForFixedPriceConfiguration>? resolved =
            registry.Resolve<QuantityForFixedPriceConfiguration>(OfferType.QuantityForFixedPrice);

        resolved.Should().BeSameAs(evaluator);
    }

    [Fact]
    public void TypedRegistryResolutionReturnsNullWhenRegisteredEvaluatorIsUntyped()
    {
        OfferEvaluatorRegistry registry = new([new UntypedEvaluator()]);

        IOfferEvaluator<QuantityForFixedPriceConfiguration>? resolved =
            registry.Resolve<QuantityForFixedPriceConfiguration>(OfferType.QuantityForFixedPrice);

        resolved.Should().BeNull();
    }

    [Fact]
    public void RegistryResolvesUntypedOfferOnlyWhenConfigurationTypeMatches()
    {
        TestEvaluator evaluator = new();
        OfferEvaluatorRegistry registry = new([evaluator]);

        registry.Resolve(CreateQuantityForFixedPriceOffer()).Should().BeSameAs(evaluator);
        registry.Resolve(CreateMismatchedConfigurationOffer()).Should().BeNull();
    }

    [Fact]
    public void RegistryRejectsDuplicateEvaluatorKeys()
    {
        Action action = () => GC.KeepAlive(new OfferEvaluatorRegistry([new TestEvaluator(), new TestEvaluator()]));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*QuantityForFixedPrice*QuantityForFixedPriceConfiguration*");
    }

    [Fact]
    public void DirectAdapterEvaluationRejectsMismatchedConfiguration()
    {
        IOfferEvaluator evaluator = new TestEvaluator();

        Action action = () => evaluator.Evaluate(BasketSnapshot.Empty, CreateMismatchedConfigurationOffer());

        action.Should().Throw<ArgumentException>()
            .WithMessage("*QuantityForFixedPriceConfiguration*");
    }

    private static OfferDefinition CreateQuantityForFixedPriceOffer() =>
        new(
            "A-3-FOR-130",
            Sku.From("A"),
            OfferType.QuantityForFixedPrice,
            OfferState.Active,
            new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m)));

    private static OfferDefinition CreateMismatchedConfigurationOffer() =>
        new(
            "A-OTHER",
            Sku.From("A"),
            OfferType.QuantityForFixedPrice,
            OfferState.Active,
            new OtherConfiguration());

    private sealed class TestEvaluator : OfferEvaluator<QuantityForFixedPriceConfiguration>
    {
        public override OfferType Type => OfferType.QuantityForFixedPrice;

        public override AppliedOffer? Evaluate(
            BasketSnapshot basket,
            OfferDefinition<QuantityForFixedPriceConfiguration> offer) =>
            null;
    }

    private sealed class UntypedEvaluator : IOfferEvaluator
    {
        public OfferType Type => OfferType.QuantityForFixedPrice;

        public Type ConfigurationType => typeof(QuantityForFixedPriceConfiguration);

        public AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition offer) => null;
    }

    private sealed record OtherConfiguration : OfferConfiguration;
}
