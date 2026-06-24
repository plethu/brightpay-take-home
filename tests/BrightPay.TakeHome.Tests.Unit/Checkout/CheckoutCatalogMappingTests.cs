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
            Quantity = 3,
            FixedPriceAmount = 130m,
        });

        offer.Configuration.Should().BeOfType<QuantityForFixedPriceConfiguration>()
            .Which.Should().BeEquivalentTo(new
            {
                Quantity = 3,
                FixedPrice = CheckoutMoney.Pounds(130m),
            });
    }

    [Fact]
    public void TransactionStartedFromCatalogWithActiveOfferStillUsesUnitPriceTotal()
    {
        CheckoutCatalogSnapshot catalog = new(
            [new ProductPrice(Sku.From("A"), CheckoutMoney.Pounds(50m))],
            [
                new OfferDefinition(
                    "A-3-FOR-130",
                    Sku.From("A"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m))),
            ]);

        BasketSnapshot basket = catalog.StartTransaction(BasketSnapshot.Empty).Scan("A").Basket;
        basket = catalog.StartTransaction(basket).Scan("A").Basket;
        basket = catalog.StartTransaction(basket).Scan("A").Basket;

        catalog.StartTransaction(basket).Total.Amount.Should().Be(150m);
    }
}
