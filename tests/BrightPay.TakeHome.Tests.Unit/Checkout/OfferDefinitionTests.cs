using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout;
using BrightPay.TakeHome.Core.Checkout.Offers;

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
            new QuantityForFixedPriceOfferConfiguration(
                new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m))));

        offer.IsActive.Should().BeTrue();
        offer.Configuration.Should().BeOfType<QuantityForFixedPriceOfferConfiguration>()
            .Which.Value.FixedPrice.Amount.Should().Be(130m);
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
    public void RegistryResolvesRegisteredEvaluatorByOfferType()
    {
        TestEvaluator evaluator = new();
        OfferEvaluatorRegistry registry = new([evaluator]);

        IOfferEvaluator? resolved = registry.Resolve(OfferType.QuantityForFixedPrice);

        resolved.Should().BeSameAs(evaluator);
    }

    private sealed class TestEvaluator : IOfferEvaluator
    {
        public OfferType Type => OfferType.QuantityForFixedPrice;

        public AppliedOffer? Evaluate(BasketSnapshot basket, OfferDefinition offer) => null;
    }
}
