using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Operations;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Web.Data.Checkout;
using Microsoft.EntityFrameworkCore;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed class CheckoutCatalogService : ICheckoutCatalogService
{
    private readonly CheckoutDbContext _dbContext;

    public CheckoutCatalogService(CheckoutDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CheckoutCatalogSnapshot> LoadActiveCatalogAsync(CancellationToken cancellationToken = default)
    {
        List<CheckoutProductEntity> products = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Offers)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        ProductPrice[] productPrices = [.. products.Select(CheckoutCatalogMapper.ToProductPrice)];

        OfferDefinition[] offers =
        [
            .. products
            .SelectMany(product => product.Offers)
            .OrderBy(offer => offer.Sku, StringComparer.Ordinal)
            .ThenBy(offer => offer.Code, StringComparer.Ordinal)
            .Select(CheckoutCatalogMapper.ToOfferDefinition),
        ];

        return new CheckoutCatalogSnapshot(productPrices, offers);
    }

    public async Task<IReadOnlyList<CheckoutCatalogItem>> LoadCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        List<CheckoutProductEntity> products = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Offers)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. products
            .Select(CheckoutCatalogMapper.ToCatalogItem),
        ];
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

    public CheckoutOperationResult Clear(BasketSnapshot basket)
    {
        CheckoutCatalogSnapshot catalog = new([], []);

        return catalog.StartTransaction(basket).Clear();
    }

    public CheckoutOperationResult Charge(BasketSnapshot basket)
    {
        CheckoutCatalogSnapshot catalog = new([], []);

        return catalog.StartTransaction(basket).Charge();
    }
}
