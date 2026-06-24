using BrightPay.TakeHome.Core.Checkout;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutCatalogItem(Sku Sku, decimal UnitPriceAmount, IReadOnlyList<CheckoutOfferItem> Offers);
