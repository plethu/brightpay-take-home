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
        CheckoutBasketCookieStore basketStore,
        CancellationToken cancellationToken)
    {
        BasketSnapshot basket = basketStore.Read(httpContext);
        CheckoutOperationResult result = await catalog.AddAsync(basket, command.SelectedSku, command.Quantity, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(httpContext, basketStore, result, command.SelectedSku);
    }

    private static async Task<IResult> IncrementAsync(
        HttpContext httpContext,
        [FromForm] CheckoutSkuCommand command,
        ICheckoutCatalogService catalog,
        CheckoutBasketCookieStore basketStore,
        CancellationToken cancellationToken)
    {
        if (Sku.TryCreate(command.Sku) is not { } sku)
        {
            return Results.Redirect(CheckoutRedirects.Error(CheckoutOperationError.UnknownSku, command.Sku));
        }

        CheckoutOperationResult result = await catalog.IncrementAsync(basketStore.Read(httpContext), sku, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(httpContext, basketStore, result, command.Sku);
    }

    private static async Task<IResult> DecrementAsync(
        HttpContext httpContext,
        [FromForm] CheckoutSkuCommand command,
        ICheckoutCatalogService catalog,
        CheckoutBasketCookieStore basketStore,
        CancellationToken cancellationToken)
    {
        if (Sku.TryCreate(command.Sku) is not { } sku)
        {
            return Results.Redirect(CheckoutRedirects.Error(CheckoutOperationError.UnknownSku, command.Sku));
        }

        CheckoutOperationResult result = await catalog.DecrementAsync(basketStore.Read(httpContext), sku, cancellationToken).ConfigureAwait(false);
        return RedirectFromMutation(httpContext, basketStore, result, command.Sku);
    }

    private static IResult Clear(
        HttpContext httpContext,
        [FromForm] CheckoutEmptyCommand command,
        ICheckoutCatalogService catalog,
        CheckoutBasketCookieStore basketStore)
    {
        ArgumentNullException.ThrowIfNull(command);
        CheckoutOperationResult result = catalog.Clear(basketStore.Read(httpContext));
        return RedirectFromMutation(httpContext, basketStore, result, feedback: CheckoutFeedbackCode.Cleared);
    }

    private static IResult Charge(
        HttpContext httpContext,
        [FromForm] CheckoutEmptyCommand command,
        ICheckoutCatalogService catalog,
        CheckoutBasketCookieStore basketStore)
    {
        ArgumentNullException.ThrowIfNull(command);
        CheckoutOperationResult result = catalog.Charge(basketStore.Read(httpContext));
        return RedirectFromMutation(httpContext, basketStore, result, feedback: CheckoutFeedbackCode.Charged);
    }

    private static IResult RedirectFromMutation(
        HttpContext httpContext,
        CheckoutBasketCookieStore basketStore,
        CheckoutOperationResult result,
        string? skuText = null,
        CheckoutFeedbackCode? feedback = null)
    {
        if (!result.Succeeded)
        {
            return Results.Redirect(CheckoutRedirects.Error(result.Error, skuText));
        }

        basketStore.Write(httpContext, result.Basket);
        return Results.Redirect(CheckoutRedirects.Success(feedback));
    }
}
