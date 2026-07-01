using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Projection;
using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed class CheckoutViewProjector
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IOfferLabelFormatter _offerLabelFormatter;

    public CheckoutViewProjector(IStringLocalizer<SharedResource> localizer, IOfferLabelFormatter offerLabelFormatter)
    {
        _localizer = localizer;
        _offerLabelFormatter = offerLabelFormatter;
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
                new CheckoutCatalogItemView(
                    item.Sku.Value,
                    SkuName(item.Sku.Value),
                    CheckoutMoney.Format(CheckoutMoney.FromPence(item.UnitPriceAmount)),
                    [
                        .. item.Offers
                            .Where(offer => offer.State == OfferState.Active)
                            .OrderBy(offer => offer.Code, StringComparer.Ordinal)
                            .Select(FormatOfferLabel),
                    ])),
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
                CheckoutMoney.Format(line.Total),
                line.Savings.Amount > 0m ? CheckoutMoney.Format(line.Subtotal) : null,
                OfferLabelsForLine(line, offersByCode))),
        ];

        IReadOnlyList<CheckoutAdjustmentView> adjustments =
        [
            .. pricedBasket.Adjustments.Select(adjustment => new CheckoutAdjustmentView(
                adjustment.Code,
                offersByCode.TryGetValue(adjustment.Code, out CheckoutOfferItem? offer)
                    ? FormatOfferLabel(offer)
                    : adjustment.Code,
                CheckoutMoney.Format(adjustment.Savings))),
        ];

        CheckoutTotalsView totals = new(
            CheckoutMoney.Format(pricedBasket.Subtotal),
            CheckoutMoney.Format(pricedBasket.Savings),
            CheckoutMoney.Format(pricedBasket.Total),
            pricedBasket.ItemCount,
            pricedBasket.LineCount,
            pricedBasket.Savings.Amount > 0m,
            pricedBasket.LineCount == 0);

        return new CheckoutViewModel(itemViews, lineViews, totals, adjustments);
    }

    public string ErrorMessage(CheckoutOperationError? error, string? skuText = null)
    {
        return error switch
        {
            CheckoutOperationError.EmptySku => _localizer["CheckoutError_EmptySku"],
            CheckoutOperationError.InvalidQuantity => _localizer["CheckoutError_InvalidQuantity"],
            CheckoutOperationError.UnknownSku => _localizer["CheckoutError_UnknownSku", skuText ?? string.Empty],
            CheckoutOperationError.EmptyBasket => _localizer["CheckoutError_EmptyBasket"],
            _ => string.Empty,
        };
    }

    private string SkuName(string sku) => _localizer[$"SkuName_{sku}"];

    private string FormatOfferLabel(CheckoutOfferItem offer) => _offerLabelFormatter.Format(offer.Type, offer.Configuration);

    private IReadOnlyList<string> OfferLabelsForLine(
        PricedBasketLine line,
        Dictionary<string, CheckoutOfferItem> offersByCode) =>
        [
            .. line.AppliedOffers
                .Select(summary => offersByCode.TryGetValue(summary.Code, out CheckoutOfferItem? offer) ? offer : null)
                .Where(offer => offer is not null)
                .Select(offer => FormatOfferLabel(offer!)),
        ];
}
