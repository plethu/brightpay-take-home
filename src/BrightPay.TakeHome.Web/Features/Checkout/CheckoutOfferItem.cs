using BrightPay.TakeHome.Core.Checkout.Offers;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutOfferItem(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    int Quantity,
    decimal FixedPriceAmount);
