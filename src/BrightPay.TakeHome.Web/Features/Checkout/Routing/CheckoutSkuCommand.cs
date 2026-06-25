using System.ComponentModel.DataAnnotations;

namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public sealed class CheckoutSkuCommand
{
    [Required]
    public string Sku { get; init; } = string.Empty;
}
