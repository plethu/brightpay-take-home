namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public sealed class CheckoutAddCommand
{
    public string? Sku { get; init; }

    public string? ManualSku { get; init; }

    public int Quantity { get; init; } = 1;

    public string SelectedSku => string.IsNullOrWhiteSpace(Sku)
        ? ManualSku ?? string.Empty
        : Sku;
}
