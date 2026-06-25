using System.ComponentModel.DataAnnotations;

namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public sealed class CheckoutAddCommand : IValidatableObject
{
    public string? Sku { get; set; }

    public string? ManualSku { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;

    public string SelectedSku => string.IsNullOrWhiteSpace(Sku)
        ? ManualSku ?? string.Empty
        : Sku;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SelectedSku))
        {
            yield return new ValidationResult(
                "A product SKU is required.",
                [nameof(ManualSku)]);
        }
    }
}
