namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

/// <summary>
/// Marker model for checkout POST actions that carry no business payload.
/// </summary>
public sealed class CheckoutPostCommand
{
    public string Submitted { get; init; } = string.Empty;
}
