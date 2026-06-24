using BrightPay.TakeHome.Core.Checkout;
using BrightPay.TakeHome.Core.Checkout.Offers;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutOfferItem(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    int Quantity,
    decimal FixedPriceAmount);
