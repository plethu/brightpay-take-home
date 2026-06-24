using BrightPay.TakeHome.Core.Checkout;
using BrightPay.TakeHome.Core.Checkout.Offers;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutCatalogSnapshot(
    IReadOnlyList<ProductPrice> ProductPrices,
    IReadOnlyList<OfferDefinition> Offers)
{
    public CheckoutTransaction StartTransaction(BasketSnapshot basket) => new(ProductPrices, basket);
}
