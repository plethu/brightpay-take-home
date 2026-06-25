using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Operations;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public interface ICheckoutCatalogService
{
    Task<CheckoutCatalogSnapshot> LoadActiveCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckoutCatalogItem>> LoadCatalogItemsAsync(CancellationToken cancellationToken = default);

    Task<CheckoutOperationResult> AddAsync(BasketSnapshot basket, string? skuText, int quantity, CancellationToken cancellationToken = default);

    Task<CheckoutOperationResult> IncrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default);

    Task<CheckoutOperationResult> DecrementAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default);

    Task<CheckoutOperationResult> RemoveLineAsync(BasketSnapshot basket, Sku sku, CancellationToken cancellationToken = default);

    CheckoutOperationResult Clear(BasketSnapshot basket);

    // Async because it prices the basket to record the sale total before clearing.
    Task<CheckoutOperationResult> ChargeAsync(BasketSnapshot basket, CancellationToken cancellationToken = default);
}
