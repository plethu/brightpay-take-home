using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed record CheckoutOfferItem(
    string Code,
    Sku Sku,
    OfferType Type,
    OfferState State,
    int Quantity,
    decimal FixedPriceAmount);
