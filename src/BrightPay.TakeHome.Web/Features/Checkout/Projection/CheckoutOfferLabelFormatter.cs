using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public sealed class CheckoutOfferLabelFormatter : IOfferLabelFormatter
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CheckoutOfferLabelFormatter(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Format(OfferType type, OfferConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return (type, configuration) switch
        {
            (OfferType.QuantityForFixedPrice, QuantityForFixedPriceConfiguration quantityOffer) =>
                _localizer[
                    "CheckoutOffer_QuantityForFixedPrice",
                    quantityOffer.Quantity,
                    CheckoutMoney.Format(quantityOffer.FixedPrice)],
            (OfferType.None, _) =>
                throw new InvalidOperationException("Offer type 'None' cannot be formatted for checkout."),
            _ => throw new InvalidOperationException($"No checkout offer label is registered for offer type '{type}'."),
        };
    }
}
