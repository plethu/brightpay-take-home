using BrightPay.TakeHome.Core.Checkout.Basket;

namespace BrightPay.TakeHome.Core.Checkout.Operations;

public sealed record CheckoutOperationResult
{
    private CheckoutOperationResult(BasketSnapshot basket, CheckoutOperationError? error)
    {
        Basket = basket;
        Error = error;
    }

    public BasketSnapshot Basket { get; }

    public CheckoutOperationError? Error { get; }

    public bool Succeeded => Error is null;

    public static CheckoutOperationResult Success(BasketSnapshot basket) => new(basket, error: null);

    public static CheckoutOperationResult Failure(BasketSnapshot basket, CheckoutOperationError error) => new(basket, error);
}
