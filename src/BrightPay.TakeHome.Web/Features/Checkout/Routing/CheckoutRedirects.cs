using BrightPay.TakeHome.Core.Checkout.Operations;

namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public static class CheckoutRedirects
{
    public static string Success(CheckoutFeedbackCode? feedback = null) =>
        feedback is { } code
            ? $"{CheckoutRoutes.Page}?feedback={code}"
            : CheckoutRoutes.Page;

    public static string Error(CheckoutOperationError? error, string? skuText = null)
    {
        CheckoutMutationErrorCode errorCode = error switch
        {
            CheckoutOperationError.EmptySku => CheckoutMutationErrorCode.EmptySku,
            CheckoutOperationError.InvalidQuantity => CheckoutMutationErrorCode.InvalidQuantity,
            CheckoutOperationError.UnknownSku => CheckoutMutationErrorCode.UnknownSku,
            CheckoutOperationError.EmptyBasket => CheckoutMutationErrorCode.EmptyBasket,
            _ => CheckoutMutationErrorCode.UnknownSku,
        };

        string skuQuery = string.IsNullOrWhiteSpace(skuText)
            ? string.Empty
            : $"&sku={Uri.EscapeDataString(skuText)}";
        return $"{CheckoutRoutes.Page}?error={errorCode}{skuQuery}";
    }
}
