using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Transactions;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutCatalogSnapshot(
    IReadOnlyList<ProductPrice> ProductPrices,
    IReadOnlyList<OfferDefinition> Offers)
{
    public CheckoutTransaction StartTransaction(BasketSnapshot basket) => new(ProductPrices, basket);
}
