using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;

namespace BrightPay.TakeHome.Web.Features.Checkout.Projection;

public interface IOfferLabelFormatter
{
    string Format(OfferType type, OfferConfiguration configuration);
}
