using System.Globalization;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Projection;
using Microsoft.Extensions.Localization;
using NodaMoney;

namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed class CheckoutViewProjector
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CheckoutViewProjector(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public CheckoutViewModel Project(
        CheckoutCatalogSnapshot catalog,
        IReadOnlyList<CheckoutCatalogItem> catalogItems,
        BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(catalogItems);
        ArgumentNullException.ThrowIfNull(basket);

        PricedBasket pricedBasket = catalog.StartTransaction(basket).Project();

        IReadOnlyList<CheckoutCatalogItemView> itemViews =
        [
            .. catalogItems.Select(item =>
            {
                CheckoutOfferItem? offer = item.Offers.FirstOrDefault(offer => offer.State == Core.Checkout.Offers.Definitions.OfferState.Active);
                return new CheckoutCatalogItemView(
                    item.Sku.Value,
                    SkuName(item.Sku.Value),
                    FormatMoney(Money.PoundSterling(item.UnitPriceAmount)),
                    offer is null ? null : FormatOfferLabel(offer));
            }),
        ];

        Dictionary<string, CheckoutOfferItem> offersByCode = catalogItems
            .SelectMany(item => item.Offers)
            .ToDictionary(offer => offer.Code, StringComparer.Ordinal);

        IReadOnlyList<CheckoutReceiptLineView> lineViews =
        [
            .. pricedBasket.Lines.Select(line => new CheckoutReceiptLineView(
                line.Sku.Value,
                SkuName(line.Sku.Value),
                line.Quantity,
                FormatMoney(line.Total),
                line.Savings.Amount > 0m ? FormatMoney(line.Subtotal) : null,
                line.AppliedOffer is not null && offersByCode.TryGetValue(line.AppliedOffer.Code, out CheckoutOfferItem? offer)
                    ? FormatOfferLabel(offer)
                    : null)),
        ];

        CheckoutTotalsView totals = new(
            FormatMoney(pricedBasket.Subtotal),
            FormatMoney(pricedBasket.Savings),
            FormatMoney(pricedBasket.Total),
            pricedBasket.ItemCount,
            pricedBasket.LineCount,
            pricedBasket.Savings.Amount > 0m,
            pricedBasket.LineCount == 0);

        return new CheckoutViewModel(itemViews, lineViews, totals);
    }

    public string ErrorMessage(Core.Checkout.Operations.CheckoutOperationError? error, string? skuText = null)
    {
        return error switch
        {
            Core.Checkout.Operations.CheckoutOperationError.EmptySku => _localizer["CheckoutError_EmptySku"],
            Core.Checkout.Operations.CheckoutOperationError.InvalidQuantity => _localizer["CheckoutError_InvalidQuantity"],
            Core.Checkout.Operations.CheckoutOperationError.UnknownSku => _localizer["CheckoutError_UnknownSku", skuText ?? string.Empty],
            Core.Checkout.Operations.CheckoutOperationError.EmptyBasket => _localizer["CheckoutError_EmptyBasket"],
            _ => string.Empty,
        };
    }

    private string SkuName(string sku) => _localizer[$"SkuName_{sku}"];

    private string FormatOfferLabel(CheckoutOfferItem offer)
    {
        return offer.Type switch
        {
            Core.Checkout.Offers.Definitions.OfferType.QuantityForFixedPrice =>
                _localizer["CheckoutOffer_QuantityForFixedPrice", offer.Quantity, FormatMoney(Money.PoundSterling(offer.FixedPriceAmount))],
            Core.Checkout.Offers.Definitions.OfferType.None =>
                throw new InvalidOperationException("Offer type 'None' cannot be formatted for checkout."),
            _ => throw new InvalidOperationException($"No checkout offer label is registered for offer type '{offer.Type}'."),
        };
    }

    private static string FormatMoney(Money money)
    {
        decimal pounds = money.Amount / 100m;
        return pounds.ToString("C", CultureInfo.GetCultureInfo("en-GB"));
    }
}
