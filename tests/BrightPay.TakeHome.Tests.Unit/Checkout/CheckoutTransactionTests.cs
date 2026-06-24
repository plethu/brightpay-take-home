using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Transactions;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class CheckoutTransactionTests
{
    [Fact]
    public void ScanBuildsBasketAndCalculatesUnitPriceTotal()
    {
        CheckoutTransaction transaction = StartEmptyTransaction();

        CheckoutOperationResult first = transaction.Scan("B");
        CheckoutOperationResult second = StartTransaction(first.Basket).Scan("A");
        CheckoutOperationResult third = StartTransaction(second.Basket).Scan("B");

        third.Succeeded.Should().BeTrue();
        third.Basket.QuantityFor(Sku.From("A")).Should().Be(1);
        third.Basket.QuantityFor(Sku.From("B")).Should().Be(2);
        StartTransaction(third.Basket).Total.Amount.Should().Be(110m);
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

    private static CheckoutTransaction StartEmptyTransaction() => StartTransaction(BasketSnapshot.Empty);

    private static CheckoutTransaction StartTransaction(BasketSnapshot basket) =>
        new(
            [
                new ProductPrice(Sku.From("A"), CheckoutMoney.Pounds(50m)),
                new ProductPrice(Sku.From("B"), CheckoutMoney.Pounds(30m)),
                new ProductPrice(Sku.From("C"), CheckoutMoney.Pounds(20m)),
                new ProductPrice(Sku.From("D"), CheckoutMoney.Pounds(15m)),
            ],
            basket);
}
