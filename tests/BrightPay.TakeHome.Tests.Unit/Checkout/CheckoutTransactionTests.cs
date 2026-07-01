using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Projection;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Transactions;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using NodaMoney;

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
                Subtotal = CheckoutMoney.FromPence(150m),
                Savings = CheckoutMoney.FromPence(20m),
                Total = CheckoutMoney.FromPence(130m),
            }, options => options.ExcludingMissingMembers());
        pricedBasket.Lines.Single().AppliedOffers.Should().ContainSingle()
            .Which.Code.Should().Be("A-3-FOR-130");
    }

    [Fact]
    public void ProjectCarriesAllSelectedLineOfferSummaries()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1)]);
        ProductPrice[] prices = [new(Sku.From("A"), CheckoutMoney.FromPence(50m))];
        OfferDefinition[] offers =
        [
            FakeOffer(
                "A-LINE-10",
                Sku.From("A"),
                OfferType.QuantityForFixedPrice,
                new LineConfiguration(),
                combinationRule: OfferCombinationRule.Stackable),
            FakeOffer(
                "A-LINE-5",
                Sku.From("A"),
                OfferType.QuantityForFixedPrice,
                new LineConfiguration(),
                combinationRule: OfferCombinationRule.Stackable),
        ];

        PricedBasket pricedBasket = new PricedBasketProjector(prices, offers, [new FakeLineEvaluator()]).Project(basket);

        pricedBasket.Lines.Should().ContainSingle()
            .Which.AppliedOffers.Select(offer => offer.Code)
            .Should().Equal("A-LINE-10", "A-LINE-5");
    }

    [Fact]
    public void ProjectKeepsBasketScopedSavingsOutOfLineTotals()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1), new BasketLine(Sku.From("B"), 1)]);
        ProductPrice[] prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
        ];
        OfferDefinition[] offers =
        [
            FakeOffer("BASKET-25", Sku.From("A"), OfferType.None, new GroupConfiguration(), OfferScope.Basket),
        ];

        PricedBasket pricedBasket = new PricedBasketProjector(prices, offers, [new FakeGroupEvaluator()]).Project(basket);

        pricedBasket.Lines.Sum(line => line.Total.Amount).Should().Be(80m);
        pricedBasket.Adjustments.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Code = "BASKET-25",
                Savings = CheckoutMoney.FromPence(25m),
            });
        pricedBasket.Total.Amount.Should().Be(55m);
    }

    [Fact]
    public void ProjectSurfacesGroupScopedOfferAsAdjustmentNotLineSavings()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1), new BasketLine(Sku.From("B"), 1)]);
        ProductPrice[] prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
        ];
        OfferDefinition[] offers =
        [
            FakeOffer("GROUP-25", Sku.From("A"), OfferType.None, new GroupConfiguration(), OfferScope.Group),
        ];

        PricedBasket pricedBasket = new PricedBasketProjector(prices, offers, [new FakeGroupEvaluator()]).Project(basket);

        // A group promotion spans several lines, so its saving is shown once as an adjustment rather
        // than split into misleading per-line discounts; per-line totals stay at their subtotals.
        pricedBasket.Lines.Sum(line => line.Total.Amount).Should().Be(80m);
        pricedBasket.Lines.SelectMany(line => line.AppliedOffers).Should().BeEmpty();
        pricedBasket.Adjustments.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Code = "GROUP-25",
                Savings = CheckoutMoney.FromPence(25m),
            });
        pricedBasket.Total.Amount.Should().Be(55m);
    }

    [Fact]
    public void PlannerStacksStackableOfferOnTopOfExclusiveClaimingSameUnit()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1)]);
        ProductPrice[] prices = [new(Sku.From("A"), CheckoutMoney.FromPence(50m))];
        OfferDefinition[] offers =
        [
            FakeOffer("A-EXCL", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer(
                "A-STACK",
                Sku.From("A"),
                OfferType.QuantityForFixedPrice,
                new LineConfiguration(),
                combinationRule: OfferCombinationRule.Stackable),
        ];

        OfferApplicationPlan plan = PlanWithFakeEvaluators(basket, prices, offers, [new FakeLineEvaluator()]);

        // The exclusive offer reserves the single unit; the stackable offer reserves nothing and so
        // applies alongside it, yielding both savings on the one unit.
        plan.Applications.Select(application => application.Code).Should().Equal("A-EXCL", "A-STACK");
        plan.TotalSavings.Should().Be(CheckoutMoney.FromPence(20m));
    }

    [Fact]
    public void PlannerSelectsSingleExclusiveOfferWhenSeveralClaimTheSameUnit()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1)]);
        ProductPrice[] prices = [new(Sku.From("A"), CheckoutMoney.FromPence(50m))];
        OfferDefinition[] offers =
        [
            FakeOffer("A-LINE-10", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("A-LINE-5", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
        ];

        OfferApplicationPlan plan = PlanWithFakeEvaluators(basket, prices, offers, [new FakeLineEvaluator()]);

        // Both exclusive offers want the one unit, so only the higher-saving one is selected.
        plan.Applications.Select(application => application.Code).Should().Equal("A-LINE-10");
        plan.TotalSavings.Should().Be(CheckoutMoney.FromPence(10m));
    }

    [Fact]
    public void QuantityOfferApplicationRecordsAffectedLineQuantity()
    {
        OfferDefinition offer = new(
            "A-3-FOR-130",
            Sku.From("A"),
            OfferType.QuantityForFixedPrice,
            OfferState.Active,
            new QuantityForFixedPriceConfiguration(3, CheckoutMoney.FromPence(130m)));
        QuantityForFixedPriceEvaluator evaluator = new();

        IReadOnlyList<OfferApplication> applications = evaluator.Evaluate(
            new BasketSnapshot([new BasketLine(Sku.From("A"), 5)]),
            offer,
            new Dictionary<Sku, ProductPrice>
            {
                [Sku.From("A")] = new ProductPrice(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            });

        applications.Should().ContainSingle()
            .Which.LineEffects.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                LineReference = "A",
                Sku = Sku.From("A"),
                QuantityConsumed = 3,
                AllocatedSavings = CheckoutMoney.FromPence(20m),
            });
    }

    [Fact]
    public void BasketPlannerSelectsGlobalBestCompatibleApplication()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1), new BasketLine(Sku.From("B"), 1)]);
        ProductPrice[] prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
        ];
        OfferDefinition[] offers =
        [
            FakeOffer("A-LINE-10", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("B-LINE-10", Sku.From("B"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("GROUP-25", Sku.From("A"), OfferType.None, new GroupConfiguration(), OfferScope.Group),
        ];
        OfferEvaluatorRegistry registry = new([new FakeLineEvaluator(), new FakeGroupEvaluator()]);

        OfferApplicationPlan plan = new BestCustomerValueOfferApplicationPlanner().Plan(
            new OfferPlanningContext(basket, prices.ToDictionary(price => price.Sku), offers, registry));

        plan.Applications.Should().ContainSingle()
            .Which.Code.Should().Be("GROUP-25");
        plan.TotalSavings.Should().Be(CheckoutMoney.FromPence(25m));
    }

    [Fact]
    public void BasketPlannerMaximizesCombinedSavingsOverLargestSingleCandidate()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1), new BasketLine(Sku.From("B"), 1)]);
        ProductPrice[] prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
        ];
        OfferDefinition[] offers =
        [
            FakeOffer("A-LINE-10", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("B-LINE-10", Sku.From("B"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("GROUP-15", Sku.From("A"), OfferType.None, new GroupConfiguration(), OfferScope.Group),
        ];

        OfferApplicationPlan plan = PlanWithFakeEvaluators(
            basket,
            prices,
            offers,
            [new FakeLineEvaluator(), new FakeGroupEvaluator()]);

        plan.Applications.Select(application => application.Code)
            .Should().Equal("A-LINE-10", "B-LINE-10");
        plan.TotalSavings.Should().Be(CheckoutMoney.FromPence(20m));
    }

    [Fact]
    public void BasketPlannerOutputIsDeterministicWhenInputsAreReordered()
    {
        BasketSnapshot basket = new([new BasketLine(Sku.From("A"), 1), new BasketLine(Sku.From("B"), 1)]);
        ProductPrice[] prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
        ];
        OfferDefinition[] offers =
        [
            FakeOffer("B-LINE-10", Sku.From("B"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
            FakeOffer("GROUP-25", Sku.From("A"), OfferType.None, new GroupConfiguration(), OfferScope.Group),
            FakeOffer("A-LINE-10", Sku.From("A"), OfferType.QuantityForFixedPrice, new LineConfiguration()),
        ];
        OfferDefinition[] reorderedOffers = [offers[2], offers[0], offers[1]];

        OfferApplicationPlan first = PlanWithFakeEvaluators(basket, prices, offers, [new FakeGroupEvaluator(), new FakeLineEvaluator()]);
        OfferApplicationPlan second = PlanWithFakeEvaluators(basket, prices, reorderedOffers, [new FakeLineEvaluator(), new FakeGroupEvaluator()]);

        first.Applications.Select(application => application.Code)
            .Should().Equal(second.Applications.Select(application => application.Code));
        first.TotalSavings.Should().Be(second.TotalSavings);
    }

    [Fact]
    public void ProjectRejectsActiveOfferWithoutRegisteredEvaluator()
    {
        CheckoutTransaction transaction = new(
            [new ProductPrice(Sku.From("A"), CheckoutMoney.FromPence(50m))],
            [
                new OfferDefinition(
                    "A-UNSUPPORTED",
                    Sku.From("A"),
                    OfferType.None,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(3, CheckoutMoney.FromPence(130m))),
            ],
            Evaluators,
            new BasketSnapshot([new BasketLine(Sku.From("A"), 3)]));

        Action act = () => transaction.Project();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No evaluator is registered for active offer 'A-UNSUPPORTED' (None).");
    }

    private static CheckoutTransaction StartEmptyTransaction() => StartTransaction(BasketSnapshot.Empty);

    private static CheckoutTransaction StartTransaction(BasketSnapshot basket) =>
        new(
            [
                new ProductPrice(Sku.From("A"), CheckoutMoney.FromPence(50m)),
                new ProductPrice(Sku.From("B"), CheckoutMoney.FromPence(30m)),
                new ProductPrice(Sku.From("C"), CheckoutMoney.FromPence(20m)),
                new ProductPrice(Sku.From("D"), CheckoutMoney.FromPence(15m)),
            ],
            [
                new OfferDefinition(
                    "A-3-FOR-130",
                    Sku.From("A"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(3, CheckoutMoney.FromPence(130m))),
                new OfferDefinition(
                    "B-2-FOR-45",
                    Sku.From("B"),
                    OfferType.QuantityForFixedPrice,
                    OfferState.Active,
                    new QuantityForFixedPriceConfiguration(2, CheckoutMoney.FromPence(45m))),
            ],
            Evaluators,
            basket);

    private static readonly IReadOnlyList<IOfferEvaluator> Evaluators = [new QuantityForFixedPriceEvaluator()];

    private static OfferApplicationPlan PlanWithFakeEvaluators(
        BasketSnapshot basket,
        IReadOnlyList<ProductPrice> prices,
        IReadOnlyList<OfferDefinition> offers,
        IReadOnlyList<IOfferEvaluator> evaluators) =>
        new BestCustomerValueOfferApplicationPlanner().Plan(
            new OfferPlanningContext(basket, prices.ToDictionary(price => price.Sku), offers, new OfferEvaluatorRegistry(evaluators)));

    private static OfferDefinition FakeOffer(
        string code,
        Sku sku,
        OfferType type,
        OfferConfiguration configuration,
        OfferScope scope = OfferScope.Line,
        OfferCombinationRule combinationRule = OfferCombinationRule.Exclusive) =>
        new(code, sku, type, OfferState.Active, configuration, scope, CombinationRule: combinationRule);

    private sealed record LineConfiguration : OfferConfiguration;

    private sealed record GroupConfiguration : OfferConfiguration;

    private sealed class FakeLineEvaluator : OfferEvaluator<LineConfiguration>
    {
        public override OfferType Type => OfferType.QuantityForFixedPrice;

        public override IReadOnlyList<OfferApplication> Evaluate(
            BasketSnapshot basket,
            OfferDefinition<LineConfiguration> offer,
            IReadOnlyDictionary<Sku, ProductPrice> prices)
        {
            return
            [
                new OfferApplication(
                    offer.Code,
                    offer.Sku,
                    offer.Type,
                    offer.Scope,
                    offer.Priority,
                    offer.CombinationRule,
                    1,
                    LineSavingFor(offer.Code),
                    [new OfferApplicationLineEffect(offer.Sku.Value, offer.Sku, 1, LineSavingFor(offer.Code))]),
            ];
        }

        private static Money LineSavingFor(string code) =>
            string.Equals(code, "A-LINE-5", StringComparison.Ordinal)
                ? CheckoutMoney.FromPence(5m)
                : CheckoutMoney.FromPence(10m);
    }

    private sealed class FakeGroupEvaluator : OfferEvaluator<GroupConfiguration>
    {
        public override OfferType Type => OfferType.None;

        public override IReadOnlyList<OfferApplication> Evaluate(
            BasketSnapshot basket,
            OfferDefinition<GroupConfiguration> offer,
            IReadOnlyDictionary<Sku, ProductPrice> prices)
        {
            return
            [
                new OfferApplication(
                    offer.Code,
                    offer.Sku,
                    offer.Type,
                    offer.Scope,
                    offer.Priority,
                    offer.CombinationRule,
                    1,
                    GroupSavingFor(offer.Code),
                    [
                        new OfferApplicationLineEffect("A", Sku.From("A"), 1, CheckoutMoney.FromPence(10m)),
                        new OfferApplicationLineEffect("B", Sku.From("B"), 1, GroupSavingFor(offer.Code) - CheckoutMoney.FromPence(10m)),
                    ]),
            ];
        }

        private static Money GroupSavingFor(string code) =>
            string.Equals(code, "GROUP-15", StringComparison.Ordinal)
                ? CheckoutMoney.FromPence(15m)
                : CheckoutMoney.FromPence(25m);
    }
}
