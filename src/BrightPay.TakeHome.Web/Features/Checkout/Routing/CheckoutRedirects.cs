using BrightPay.TakeHome.Core.Checkout.Operations;

namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public static class CheckoutRedirects
{
    public static string Success(CheckoutFeedbackCode? feedback = null, string? skuText = null)
    {
        if (feedback is not { } code)
        {
            return CheckoutRoutes.Page;
        }

        string skuQuery = string.IsNullOrWhiteSpace(skuText)
            ? string.Empty
            : $"&sku={Uri.EscapeDataString(skuText)}";
        return $"{CheckoutRoutes.Page}?feedback={code}{skuQuery}";
    }

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
