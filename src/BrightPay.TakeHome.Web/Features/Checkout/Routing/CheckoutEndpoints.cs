using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Web.Features.Checkout.State;
using Microsoft.AspNetCore.Mvc;

namespace BrightPay.TakeHome.Web.Features.Checkout.Routing;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost(CheckoutRoutes.Add, AddAsync);
        _ = endpoints.MapPost(CheckoutRoutes.Increment, IncrementAsync);
        _ = endpoints.MapPost(CheckoutRoutes.Decrement, DecrementAsync);
        _ = endpoints.MapPost(CheckoutRoutes.Clear, Clear);
        _ = endpoints.MapPost(CheckoutRoutes.Charge, Charge);

        return endpoints;
    }

    private static async Task<IResult> AddAsync(
        HttpContext httpContext,
        [FromForm] CheckoutAddCommand command,
        ICheckoutCatalogService catalog,
        ICheckoutBasketStore basketStore,
        CancellationToken cancellationToken)
    {
        string sessionId = GetSessionId(httpContext);
        BasketSnapshot basket = basketStore.Read(sessionId);
        CheckoutOperationResult result = await catalog.AddAsync(basket, command.SelectedSku, command.Quantity, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(sessionId, basketStore, result, command.SelectedSku, CheckoutFeedbackCode.Added, command.SelectedSku);
    }

    private static async Task<IResult> IncrementAsync(
        HttpContext httpContext,
        [FromForm] CheckoutSkuCommand command,
        ICheckoutCatalogService catalog,
        ICheckoutBasketStore basketStore,
        CancellationToken cancellationToken)
    {
        if (Sku.TryCreate(command.Sku) is not { } sku)
        {
            return Results.Redirect(CheckoutRedirects.Error(CheckoutOperationError.UnknownSku, command.Sku));
        }

        string sessionId = GetSessionId(httpContext);
        CheckoutOperationResult result = await catalog.IncrementAsync(basketStore.Read(sessionId), sku, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(sessionId, basketStore, result, command.Sku);
    }

    private static async Task<IResult> DecrementAsync(
        HttpContext httpContext,
        [FromForm] CheckoutSkuCommand command,
        ICheckoutCatalogService catalog,
        ICheckoutBasketStore basketStore,
        CancellationToken cancellationToken)
    {
        if (Sku.TryCreate(command.Sku) is not { } sku)
        {
            return Results.Redirect(CheckoutRedirects.Error(CheckoutOperationError.UnknownSku, command.Sku));
        }

        string sessionId = GetSessionId(httpContext);
        CheckoutOperationResult result = await catalog.DecrementAsync(basketStore.Read(sessionId), sku, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(sessionId, basketStore, result, command.Sku);
    }

    private static IResult Clear(
        HttpContext httpContext,
        [FromForm] CheckoutEmptyCommand command,
        ICheckoutCatalogService catalog,
        ICheckoutBasketStore basketStore)
    {
        ArgumentNullException.ThrowIfNull(command);
        string sessionId = GetSessionId(httpContext);
        CheckoutOperationResult result = catalog.Clear(basketStore.Read(sessionId));
        return RedirectFromMutation(sessionId, basketStore, result, feedback: CheckoutFeedbackCode.Cleared);
    }

    private static async Task<IResult> Charge(
        HttpContext httpContext,
        [FromForm] CheckoutEmptyCommand command,
        ICheckoutCatalogService catalog,
        ICheckoutBasketStore basketStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        string sessionId = GetSessionId(httpContext);
        CheckoutOperationResult result = await catalog.ChargeAsync(basketStore.Read(sessionId), cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(sessionId, basketStore, result, feedback: CheckoutFeedbackCode.Charged);
    }

    private static IResult RedirectFromMutation(
        string sessionId,
        ICheckoutBasketStore basketStore,
        CheckoutOperationResult result,
        string? skuText = null,
        CheckoutFeedbackCode? feedback = null,
        string? feedbackSkuText = null)
    {
        if (!result.Succeeded)
        {
            return Results.Redirect(CheckoutRedirects.Error(result.Error, skuText));
        }

        basketStore.Write(sessionId, result.Basket);
        return Results.Redirect(CheckoutRedirects.Success(feedback, feedbackSkuText));
    }

    private static string GetSessionId(HttpContext httpContext) =>
        httpContext.Items[CheckoutSession.ItemsKey] as string ?? string.Empty;
}
