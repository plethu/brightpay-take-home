using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Projection;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Transactions;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class CheckoutTransactionTests
{
    [Fact]
    public void ScanBuildsBasketAndCalculatesOfferAwareTotal()
    {
        CheckoutTransaction transaction = StartEmptyTransaction();

        CheckoutOperationResult first = transaction.Scan("B");
        CheckoutOperationResult second = StartTransaction(first.Basket).Scan("A");
        CheckoutOperationResult third = StartTransaction(second.Basket).Scan("B");

        third.Succeeded.Should().BeTrue();
        third.Basket.QuantityFor(Sku.From("A")).Should().Be(1);
        third.Basket.QuantityFor(Sku.From("B")).Should().Be(2);
        StartTransaction(third.Basket).Total.Amount.Should().Be(95m);
    }

    [Theory]
    [InlineData("AAA", 130)]
    [InlineData("AAAA", 180)]
    [InlineData("AAAAAA", 260)]
    [InlineData("BB", 45)]
    [InlineData("BAB", 95)]
    public void TotalAppliesQuantityOffersRepeatedly(string scanOrder, decimal expectedTotal)
    {
        ArgumentNullException.ThrowIfNull(scanOrder);

        BasketSnapshot basket = BasketSnapshot.Empty;

        foreach (char sku in scanOrder)
        {
            basket = StartTransaction(basket).Scan(sku.ToString()).Basket;
        }

        StartTransaction(basket).Total.Amount.Should().Be(expectedTotal);
    }

    [Fact]
    public void AddSupportsExplicitQuantity()
    {
        CheckoutOperationResult result = StartEmptyTransaction().Add("A", 3);

        result.Succeeded.Should().BeTrue();
        result.Basket.QuantityFor(Sku.From("A")).Should().Be(3);
        StartTransaction(result.Basket).Total.Amount.Should().Be(130m);
    }

    [Fact]
    public void AddRejectsInvalidQuantityWithoutChangingBasket()
    {
        CheckoutOperationResult result = StartEmptyTransaction().Add("A", 0);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(CheckoutOperationError.InvalidQuantity);
        result.Basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IncrementAddsExistingOrKnownSku()
    {
        BasketSnapshot basket = StartEmptyTransaction().Scan("C").Basket;

        CheckoutOperationResult result = StartTransaction(basket).Increment(Sku.From("C"));

        result.Succeeded.Should().BeTrue();
        result.Basket.QuantityFor(Sku.From("C")).Should().Be(2);
    }

    [Fact]
    public void DecrementRemovesLineAtQuantityOne()
    {
        BasketSnapshot basket = StartEmptyTransaction().Scan("D").Basket;

        CheckoutOperationResult result = StartTransaction(basket).Decrement(Sku.From("D"));

        result.Succeeded.Should().BeTrue();
        result.Basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ScanRejectsEmptySkuWithoutChangingBasket()
    {
        CheckoutTransaction transaction = StartEmptyTransaction();

        CheckoutOperationResult result = transaction.Scan(" ");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(CheckoutOperationError.EmptySku);
        result.Basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ScanRejectsUnknownSkuWithoutChangingBasket()
    {
        BasketSnapshot basket = StartEmptyTransaction().Scan("A").Basket;

        CheckoutOperationResult result = StartTransaction(basket).Scan("Z");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(CheckoutOperationError.UnknownSku);
        result.Basket.Should().BeSameAs(basket);
    }

    [Fact]
    public void RemoveLineRemovesAllQuantityForSku()
    {
        BasketSnapshot basket = StartEmptyTransaction().Scan("B").Basket;
        basket = StartTransaction(basket).Scan("B").Basket;
        basket = StartTransaction(basket).Scan("A").Basket;

        CheckoutOperationResult result = StartTransaction(basket).RemoveLine(Sku.From("B"));

        result.Succeeded.Should().BeTrue();
        result.Basket.QuantityFor(Sku.From("B")).Should().Be(0);
        result.Basket.QuantityFor(Sku.From("A")).Should().Be(1);
    }

    [Fact]
    public void ClearOnEmptyBasketReturnsNonFatalError()
    {
        CheckoutOperationResult result = StartEmptyTransaction().Clear();

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(CheckoutOperationError.EmptyBasket);
        result.Basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ProjectReturnsSubtotalSavingsTotalAndLineOffer()
    {
        BasketSnapshot basket = StartEmptyTransaction().Add("A", 3).Basket;

        PricedBasket pricedBasket = StartTransaction(basket).Project();

        pricedBasket.Subtotal.Amount.Should().Be(150m);
        pricedBasket.Savings.Amount.Should().Be(20m);
        pricedBasket.Total.Amount.Should().Be(130m);
        pricedBasket.Lines.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Quantity = 3,
                Subtotal = CheckoutMoney.Pounds(150m),
                Savings = CheckoutMoney.Pounds(20m),
                Total = CheckoutMoney.Pounds(130m),
            }, options => options.ExcludingMissingMembers());
        pricedBasket.Lines.Single().AppliedOffer!.Code.Should().Be("A-3-FOR-130");
    }

    [Fact]
    public void ProjectRejectsActiveOfferWithoutRegisteredEvaluator()
    {
        CheckoutTransaction transaction = new(
            [new ProductPrice(Sku.From("A"), CheckoutMoney.Pounds(50m))],
            [
                new OfferDefinition(
                    "A-UNSUPPORTED",
                    Sku.From("A"),
                    OfferType.None,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m))),
            ],
            new BasketSnapshot([new BasketLine(Sku.From("A"), 3)]));

        Action act = () => transaction.Project();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No evaluator is registered for active offer 'A-UNSUPPORTED' (None).");
    }

    private static CheckoutTransaction StartEmptyTransaction() => StartTransaction(BasketSnapshot.Empty);

    private static CheckoutTransaction StartTransaction(BasketSnapshot basket) =>
        new(
            [
                new ProductPrice(Sku.From("A"), CheckoutMoney.Pounds(50m)),
                new ProductPrice(Sku.From("B"), CheckoutMoney.Pounds(30m)),
                new ProductPrice(Sku.From("C"), CheckoutMoney.Pounds(20m)),
                new ProductPrice(Sku.From("D"), CheckoutMoney.Pounds(15m)),
            ],
            [
                new OfferDefinition(
                    "A-3-FOR-130",
                    Sku.From("A"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m))),
                new OfferDefinition(
                    "B-2-FOR-45",
                    Sku.From("B"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(2, CheckoutMoney.Pounds(45m))),
            ],
            basket);
}
