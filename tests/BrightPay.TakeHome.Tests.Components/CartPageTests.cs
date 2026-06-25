using System.Reflection;
using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Web;
using BrightPay.TakeHome.Web.Components.Checkout;
using BrightPay.TakeHome.Web.Components.Pages;
using BrightPay.TakeHome.Web.Features.Checkout;
using BrightPay.TakeHome.Web.Features.Checkout.Projection;
using BrightPay.TakeHome.Web.Features.Checkout.Routing;
using BrightPay.TakeHome.Web.Features.Checkout.State;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Tests.Components;

public sealed class CartPageTests : BunitContext
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly FakeCheckoutBasketStore _basketStore = new();
    private readonly DefaultHttpContext _httpContext = new();

    public CartPageTests()
    {
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IHttpContextAccessor>(_ => new HttpContextAccessor
        {
            HttpContext = _httpContext,
        });
        Services.AddSingleton<ICheckoutBasketStore>(_basketStore);
        Services.AddScoped<CheckoutViewProjector>();
        Services.AddSingleton<ICheckoutCatalogService>(new FakeCheckoutCatalogService());
        Services.AddSingleton(CreateEmptyPersistentState());
        _localizer = Services.GetRequiredService<IStringLocalizer<SharedResource>>();
    }

    [Fact]
    public void CheckoutRendersLocalizedCatalogPricesAndOffers()
    {
        IRenderedComponent<CheckoutPage> component = Render<CheckoutPage>();

        component.FindAll(".checkout-title").Should().BeEmpty();
        component.Find("#add-hd").TextContent.Should().Be(_localizer["CheckoutAddHeading"].Value);
        component.Find("#sale-hd").TextContent.Should().Be(_localizer["CheckoutCurrentSale"].Value);
        component.Markup.Should().Contain(_localizer["SkuName_A"].Value);
        component.Markup.Should().Contain("£0.50");
        component.Markup.Should().Contain(_localizer["CheckoutOffer_QuantityForFixedPrice", 3, "£1.30"].Value);
        component.Markup.Should().Contain(_localizer["CheckoutEmptyTitle"].Value);
    }

    [Fact]
    public void AddPadStartsWithDisabledQuantityDecrement()
    {
        IRenderedComponent<AddPad> component =
            Render<AddPad>(parameters => parameters
            .Add(component => component.Catalog, CatalogViews));

        component.Find($".qty-step[aria-label='{_localizer["CheckoutDecreaseQuantity"].Value}']")
            .HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void SingleItemReceiptLineUsesIconOnlyRemoveControl()
    {
        IRenderedComponent<QuantityStepper> component =
            Render<QuantityStepper>(parameters => parameters
            .Add(component => component.Sku, "A")
            .Add(component => component.Name, _localizer["SkuName_A"].Value)
            .Add(component => component.Quantity, 1));

        AngleSharp.Dom.IElement removeButton = component.Find("[data-act='dec']");

        removeButton.GetAttribute("aria-label").Should().Be(_localizer["CheckoutRemoveLineLabel", _localizer["SkuName_A"].Value].Value);
        removeButton.TextContent.Should().NotContain(_localizer["CheckoutRemoveGlyph"].Value);
        removeButton.QuerySelector("svg[aria-hidden='true']").Should().NotBeNull();
    }

    [Fact]
    public void CheckoutRendersAddedToastFromFeedbackQuery()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/cart?feedback={nameof(CheckoutFeedbackCode.Added)}&sku=A");

        IRenderedComponent<CheckoutPage> component = Render<CheckoutPage>();

        component.Find(".toast").TextContent.Should().Contain(_localizer["CheckoutToast_Added", _localizer["SkuName_A"].Value].Value);
    }

    [Fact]
    public void CheckoutRendersBasketFromStore()
    {
        _httpContext.Items[CheckoutSession.ItemsKey] = "test-session";
        _basketStore.Write("test-session", new BasketSnapshot([new BasketLine(Sku.From("A"), 2)]));

        IRenderedComponent<CheckoutPage> component = Render<CheckoutPage>();

        component.Markup.Should().Contain(_localizer["SkuName_A"].Value);
        component.FindAll(".sale-line").Should().HaveCount(1);
    }

    // PersistentComponentState has an internal constructor; reflection is the only way to create
    // a no-op instance for bUnit tests. TryTakeFromJson always returns false on a fresh instance,
    // so the component falls back to HttpContext.Items for the session ID.
    private static PersistentComponentState CreateEmptyPersistentState()
    {
        ConstructorInfo ctor = typeof(PersistentComponentState)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        object[] args = [.. ctor.GetParameters().Select(p => MakeArg(p.ParameterType))];
        return (PersistentComponentState)ctor.Invoke(args);
    }

    private static object MakeArg(Type t) =>
        t.IsInterface || t.IsAbstract
            ? new Dictionary<string, byte[]>(StringComparer.Ordinal)
            : Activator.CreateInstance(t)!;

    private static readonly CheckoutCatalogItemView[] CatalogViews =
    [
        new("A", "Apple", "£0.50", "3 for £1.30"),
        new("B", "Banana", "£0.30", "2 for £0.45"),
    ];

    private sealed class FakeCheckoutBasketStore : ICheckoutBasketStore
    {
        private readonly Dictionary<string, BasketSnapshot> _store = [];

        public BasketSnapshot Read(string sessionId) =>
            _store.TryGetValue(sessionId, out BasketSnapshot? basket) ? basket : BasketSnapshot.Empty;

        public void Write(string sessionId, BasketSnapshot basket) =>
            _store[sessionId] = basket;

        public void Clear(string sessionId) => _store.Remove(sessionId);
    }

    private sealed class FakeCheckoutCatalogService : ICheckoutCatalogService
    {
        private static readonly ProductPrice[] Prices =
        [
            new(Sku.From("A"), CheckoutMoney.FromPence(50m)),
            new(Sku.From("B"), CheckoutMoney.FromPence(30m)),
            new(Sku.From("C"), CheckoutMoney.FromPence(20m)),
            new(Sku.From("D"), CheckoutMoney.FromPence(15m)),
        ];

        private static readonly OfferDefinition[] Offers =
        [
            new(
                "A-3-FOR-130",
                Sku.From("A"),
                OfferType.QuantityForFixedPrice,
                OfferState.Active,
                new QuantityForFixedPriceConfiguration(3, CheckoutMoney.FromPence(130m))),
            new(
                "B-2-FOR-45",
                Sku.From("B"),
                OfferType.QuantityForFixedPrice,
                OfferState.Active,
                new QuantityForFixedPriceConfiguration(2, CheckoutMoney.FromPence(45m))),
        ];

        public Task<CheckoutCatalogSnapshot> LoadActiveCatalogAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators));

        public Task<IReadOnlyList<CheckoutCatalogItem>> LoadCatalogItemsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CheckoutCatalogItem>>(
                [
                    new(Sku.From("A"), 50m, [new CheckoutOfferItem("A-3-FOR-130", Sku.From("A"), OfferType.QuantityForFixedPrice, OfferState.Active, 3, 130m)]),
                    new(Sku.From("B"), 30m, [new CheckoutOfferItem("B-2-FOR-45", Sku.From("B"), OfferType.QuantityForFixedPrice, OfferState.Active, 2, 45m)]),
                    new(Sku.From("C"), 20m, []),
                    new(Sku.From("D"), 15m, []),
                ]);

        public Task<CheckoutOperationResult> AddAsync(BasketSnapshot basket, string? skuText, int quantity, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).Add(skuText, quantity));

        public Task<CheckoutOperationResult> IncrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).Increment(sku));

        public Task<CheckoutOperationResult> DecrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).Decrement(sku));

        public Task<CheckoutOperationResult> RemoveLineAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).RemoveLine(sku));

        public CheckoutOperationResult Clear(BasketSnapshot basket) =>
            new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).Clear();

        public Task<CheckoutOperationResult> ChargeAsync(BasketSnapshot basket, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers, Evaluators).StartTransaction(basket).Charge());

        private static readonly IReadOnlyList<IOfferEvaluator> Evaluators = [new QuantityForFixedPriceEvaluator()];
    }
}
