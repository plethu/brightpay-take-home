using System.Text.Json;
using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Web.Data.Checkout;
using BrightPay.TakeHome.Web.Features.Checkout;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class CheckoutCatalogMappingTests
{
    [Fact]
    public void MapperLoadsQuantityForFixedPriceOfferConfiguration()
    {
        OfferDefinition offer = CheckoutCatalogMapper.ToOfferDefinition(new CheckoutOfferEntity
        {
            Code = "A-3-FOR-130",
            Sku = "A",
            Type = (int)OfferType.QuantityForFixedPrice,
            State = (int)OfferState.Active,
            Scope = (int)OfferScope.Basket,
            Priority = 5,
            CombinationRule = (int)OfferCombinationRule.Stackable,
            ConfigurationVersion = 1,
            ConfigurationJson = QuantityOfferConfigurationJson(3, CheckoutMoney.CurrencyCode, 130m),
        });

        offer.Configuration.Should().BeOfType<QuantityForFixedPriceConfiguration>()
            .Which.Should().BeEquivalentTo(new
            {
                Quantity = 3,
                FixedPrice = CheckoutMoney.FromPence(130m),
            });
        offer.Scope.Should().Be(OfferScope.Basket);
        offer.Priority.Should().Be(5);
        offer.CombinationRule.Should().Be(OfferCombinationRule.Stackable);
    }

    [Fact]
    public void MapperRejectsUnknownOfferType()
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(type: 999));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unknown offer type*");
    }

    [Fact]
    public void MapperRejectsUnknownOfferScope()
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(scope: 999));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unknown offer scope*");
    }

    [Fact]
    public void MapperRejectsUnknownOfferCombinationRule()
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(combinationRule: 999));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unknown offer combination rule*");
    }

    [Fact]
    public void MapperRejectsUnknownConfigurationVersion()
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(configurationVersion: 2));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported QuantityForFixedPrice configuration version '2'*");
    }

    [Fact]
    public void MapperRejectsMalformedConfigurationJson()
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(configurationJson: "{"));

        action.Should().Throw<JsonException>();
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurationJson))]
    public void MapperRejectsSemanticallyInvalidConfigurationJson(string configurationJson)
    {
        Action action = () => CheckoutCatalogMapper.ToOfferDefinition(OfferEntity(configurationJson: configurationJson));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TransactionStartedFromCatalogWithActiveOfferUsesOfferTotal()
    {
        CheckoutCatalogSnapshot catalog = new(
            [new ProductPrice(Sku.From("A"), CheckoutMoney.FromPence(50m))],
            [
                new OfferDefinition(
                    "A-3-FOR-130",
                    Sku.From("A"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
            new QuantityForFixedPriceConfiguration(3, CheckoutMoney.FromPence(130m))),
            ],
            [new QuantityForFixedPriceEvaluator()]);

        BasketSnapshot basket = catalog.StartTransaction(BasketSnapshot.Empty).Scan("A").Basket;
        basket = catalog.StartTransaction(basket).Scan("A").Basket;
        basket = catalog.StartTransaction(basket).Scan("A").Basket;

        catalog.StartTransaction(basket).Total.Amount.Should().Be(130m);
    }

    private static CheckoutOfferEntity OfferEntity(
        int type = (int)OfferType.QuantityForFixedPrice,
        int scope = (int)OfferScope.Line,
        int combinationRule = (int)OfferCombinationRule.Exclusive,
        int configurationVersion = 1,
        string configurationJson = "") =>
        new()
        {
            Code = "A-3-FOR-130",
            Sku = "A",
            Type = type,
            State = (int)OfferState.Active,
            Scope = scope,
            Priority = 0,
            CombinationRule = combinationRule,
            ConfigurationVersion = configurationVersion,
            ConfigurationJson = string.IsNullOrEmpty(configurationJson)
                ? QuantityOfferConfigurationJson(3, CheckoutMoney.CurrencyCode, 130m)
                : configurationJson,
        };

    public static TheoryData<string> InvalidConfigurationJson =>
        [
            QuantityOfferConfigurationJson(0, CheckoutMoney.CurrencyCode, 130m),
            QuantityOfferConfigurationJson(3, CheckoutMoney.CurrencyCode, -1m),
            QuantityOfferConfigurationJson(3, "USD", 130m),
        ];

    private static string QuantityOfferConfigurationJson(int quantity, string currency, decimal minorUnits) =>
        JsonSerializer.Serialize(new
        {
            quantity,
            fixedPrice = new
            {
                currency,
                minorUnits,
            },
        });
}
