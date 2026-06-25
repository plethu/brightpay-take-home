using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Projection;
using BrightPay.TakeHome.Web.Data.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed partial class CheckoutCatalogService : ICheckoutCatalogService
{
    private const string CatalogSnapshotCacheKey = "checkout.catalog.snapshot";
    private const string CatalogItemsCacheKey = "checkout.catalog.items";

    // The catalog is read on every mutation; cache briefly so a key-tap does not re-query the
    // whole catalog. Kept short so price/offer edits still propagate without explicit invalidation.
    private static readonly TimeSpan CatalogCacheLifetime = TimeSpan.FromSeconds(30);

    private readonly CheckoutDbContext _dbContext;
    private readonly IReadOnlyList<IOfferEvaluator> _evaluators;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CheckoutCatalogService> _logger;

    public CheckoutCatalogService(
        CheckoutDbContext dbContext,
        IEnumerable<IOfferEvaluator> evaluators,
        IMemoryCache cache,
        ILogger<CheckoutCatalogService> logger)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        _dbContext = dbContext;
        _evaluators = [.. evaluators];
        _cache = cache;
        _logger = logger;
    }

    public async Task<CheckoutCatalogSnapshot> LoadActiveCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(CatalogSnapshotCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CatalogCacheLifetime;

            List<CheckoutProductEntity> products = await LoadActiveProductsAsync(cancellationToken).ConfigureAwait(false);

            ProductPrice[] productPrices = [.. products.Select(CheckoutCatalogMapper.ToProductPrice)];

            OfferDefinition[] offers =
            [
                .. products
                .SelectMany(product => product.Offers)
                .OrderBy(offer => offer.Sku, StringComparer.Ordinal)
                .ThenBy(offer => offer.Code, StringComparer.Ordinal)
                .Select(CheckoutCatalogMapper.ToOfferDefinition),
            ];

            return new CheckoutCatalogSnapshot(productPrices, offers, _evaluators);
        }).ConfigureAwait(false) ?? new CheckoutCatalogSnapshot([], [], _evaluators);
    }

    public async Task<IReadOnlyList<CheckoutCatalogItem>> LoadCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(CatalogItemsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CatalogCacheLifetime;

            List<CheckoutProductEntity> products = await LoadActiveProductsAsync(cancellationToken).ConfigureAwait(false);

            return (IReadOnlyList<CheckoutCatalogItem>)[.. products.Select(CheckoutCatalogMapper.ToCatalogItem)];
        }).ConfigureAwait(false) ?? [];
    }

    public async Task<CheckoutOperationResult> AddAsync(
        BasketSnapshot basket,
        string? skuText,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);

        return catalog.StartTransaction(basket).Add(skuText, quantity);
    }

    public async Task<CheckoutOperationResult> IncrementAsync(
        BasketSnapshot basket,
        Sku sku,
        CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);

        return catalog.StartTransaction(basket).Increment(sku);
    }

    public async Task<CheckoutOperationResult> DecrementAsync(
        BasketSnapshot basket,
        Sku sku,
        CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);

        return catalog.StartTransaction(basket).Decrement(sku);
    }

    public async Task<CheckoutOperationResult> RemoveLineAsync(
        BasketSnapshot basket,
        Sku sku,
        CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);

        return catalog.StartTransaction(basket).RemoveLine(sku);
    }

    // Clear is catalog-independent (it only empties the basket), so it needs no priced catalog.
    public CheckoutOperationResult Clear(BasketSnapshot basket) =>
        new CheckoutCatalogSnapshot([], [], _evaluators).StartTransaction(basket).Clear();

    public async Task<CheckoutOperationResult> ChargeAsync(BasketSnapshot basket, CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);
        PricedBasket priced = catalog.StartTransaction(basket).Project();
        CheckoutOperationResult result = catalog.StartTransaction(basket).Charge();

        // Payment capture and receipt persistence are out of scope; record the sale to the log so a
        // charge is not a silent void. The total is in GBP minor units (pence).
        if (result.Succeeded)
        {
            LogCharged(_logger, priced.LineCount, priced.ItemCount, priced.Total.Amount);
        }

        return result;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Checkout charged: {LineCount} lines, {ItemCount} items, total {TotalPence} pence.")]
    private static partial void LogCharged(ILogger logger, int lineCount, int itemCount, decimal totalPence);

    private Task<List<CheckoutProductEntity>> LoadActiveProductsAsync(CancellationToken cancellationToken) =>
        _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Offers)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Sku)
            .ToListAsync(cancellationToken);
}
