using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Web;
using BrightPay.TakeHome.Web.Components.Pages;
using BrightPay.TakeHome.Web.Features.Checkout;
using BrightPay.TakeHome.Web.Features.Checkout.Projection;
using BrightPay.TakeHome.Web.Features.Checkout.State;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Tests.Components;

public sealed class CartPageTests : BunitContext
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CartPageTests()
    {
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
        Services.AddDataProtection();
        Services.AddSingleton<IHttpContextAccessor>(_ => new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        });
        Services.AddScoped<CheckoutBasketCookieStore>();
        Services.AddScoped<CheckoutViewProjector>();
        Services.AddSingleton<ICheckoutCatalogService>(new FakeCheckoutCatalogService());
        _localizer = Services.GetRequiredService<IStringLocalizer<SharedResource>>();
    }

    [Fact]
    public void CheckoutRendersLocalizedCatalogPricesAndOffers()
    {
        IRenderedComponent<CheckoutPage> component = Render<CheckoutPage>();

        component.Find("h1").TextContent.Should().Be(_localizer["CartHeading"].Value);
        component.Markup.Should().Contain(_localizer["SkuName_A"].Value);
        component.Markup.Should().Contain("£0.50");
        component.Markup.Should().Contain(_localizer["CheckoutOffer_QuantityForFixedPrice", 3, "£1.30"].Value);
        component.Markup.Should().Contain(_localizer["CheckoutEmptyTitle"].Value);
    }

    private sealed class FakeCheckoutCatalogService : ICheckoutCatalogService
    {
        private static readonly ProductPrice[] Prices =
        [
            new(Sku.From("A"), CheckoutMoney.Pounds(50m)),
            new(Sku.From("B"), CheckoutMoney.Pounds(30m)),
            new(Sku.From("C"), CheckoutMoney.Pounds(20m)),
            new(Sku.From("D"), CheckoutMoney.Pounds(15m)),
        ];

        private static readonly OfferDefinition[] Offers =
        [
            new(
                "A-3-FOR-130",
                Sku.From("A"),
                OfferType.QuantityForFixedPrice,
                OfferState.Active,
                new QuantityForFixedPriceConfiguration(3, CheckoutMoney.Pounds(130m))),
            new(
                "B-2-FOR-45",
                Sku.From("B"),
                OfferType.QuantityForFixedPrice,
                OfferState.Active,
                new QuantityForFixedPriceConfiguration(2, CheckoutMoney.Pounds(45m))),
        ];

        public Task<CheckoutCatalogSnapshot> LoadActiveCatalogAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers));

        public Task<IReadOnlyList<CheckoutCatalogItem>> LoadCatalogItemsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CheckoutCatalogItem>>(
                [
                    new(Sku.From("A"), 50m, [new CheckoutOfferItem("A-3-FOR-130", Sku.From("A"), OfferType.QuantityForFixedPrice, OfferState.Active, 3, 130m)]),
                    new(Sku.From("B"), 30m, [new CheckoutOfferItem("B-2-FOR-45", Sku.From("B"), OfferType.QuantityForFixedPrice, OfferState.Active, 2, 45m)]),
                    new(Sku.From("C"), 20m, []),
                    new(Sku.From("D"), 15m, []),
                ]);

        public Task<CheckoutOperationResult> AddAsync(BasketSnapshot basket, string? skuText, int quantity, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).Add(skuText, quantity));

        public Task<CheckoutOperationResult> IncrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).Increment(sku));

        public Task<CheckoutOperationResult> DecrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).Decrement(sku));

        public Task<CheckoutOperationResult> RemoveLineAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).RemoveLine(sku));

        public CheckoutOperationResult Clear(BasketSnapshot basket) =>
            new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).Clear();

        public CheckoutOperationResult Charge(BasketSnapshot basket) =>
            new CheckoutCatalogSnapshot(Prices, Offers).StartTransaction(basket).Charge();
    }
}
