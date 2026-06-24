using BrightPay.TakeHome.Core.Checkout;
using BrightPay.TakeHome.Core.Checkout.Offers;
using BrightPay.TakeHome.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BrightPay.TakeHome.Web.Features.Checkout;

public sealed class CheckoutCatalogService
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

        ProductPrice[] productPrices =
        [
            .. products.Select(product => new ProductPrice(Sku.From(product.Sku), CheckoutMoney.Pounds(product.UnitPriceAmount))),
        ];

        OfferDefinition[] offers =
        [
            .. products
            .SelectMany(product => product.Offers)
            .OrderBy(offer => offer.Sku, StringComparer.Ordinal)
            .ThenBy(offer => offer.Code, StringComparer.Ordinal)
            .Select(MapOffer),
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
            .Select(product => new CheckoutCatalogItem(
                Sku.From(product.Sku),
                product.UnitPriceAmount,
                [
                    .. product.Offers
                    .OrderBy(offer => offer.Code, StringComparer.Ordinal)
                    .Select(MapOfferItem),
                ])),
        ];
    }

    public async Task<CheckoutOperationResult> ScanAsync(
        BasketSnapshot basket,
        string? skuText,
        CancellationToken cancellationToken = default)
    {
        CheckoutCatalogSnapshot catalog = await LoadActiveCatalogAsync(cancellationToken).ConfigureAwait(false);

        return catalog.StartTransaction(basket).Scan(skuText);
    }

    public static CheckoutOperationResult RemoveLine(BasketSnapshot basket, Sku sku)
    {
        CheckoutCatalogSnapshot catalog = new([], []);

        return catalog.StartTransaction(basket).RemoveLine(sku);
    }

    public static CheckoutOperationResult Clear(BasketSnapshot basket)
    {
        CheckoutCatalogSnapshot catalog = new([], []);

        return catalog.StartTransaction(basket).Clear();
    }

    private static CheckoutOfferItem MapOfferItem(CheckoutOfferEntity offer) =>
        new(
            offer.Code,
            Sku.From(offer.Sku),
            (OfferType)offer.Type,
            (OfferState)offer.State,
            offer.Quantity,
            offer.FixedPriceAmount);

    private static OfferDefinition MapOffer(CheckoutOfferEntity offer) =>
        new(
            offer.Code,
            Sku.From(offer.Sku),
            (OfferType)offer.Type,
            (OfferState)offer.State,
            new QuantityForFixedPriceOfferConfiguration(
                new QuantityForFixedPriceConfiguration(
                    offer.Quantity,
                    CheckoutMoney.Pounds(offer.FixedPriceAmount))));
}
