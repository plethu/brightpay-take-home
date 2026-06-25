using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Web.Data.Checkout;
using NodaMoney;
using Riok.Mapperly.Abstractions;

namespace BrightPay.TakeHome.Web.Features.Checkout;

[Mapper]
internal static partial class CheckoutCatalogMapper
{
    [MapProperty(nameof(CheckoutProductEntity.UnitPriceAmount), nameof(ProductPrice.UnitPrice))]
    [MapperIgnoreSource(nameof(CheckoutProductEntity.IsActive))]
    [MapperIgnoreSource(nameof(CheckoutProductEntity.Offers))]
    public static partial ProductPrice ToProductPrice(CheckoutProductEntity product);

    // Configuration has no direct structural equivalent in the entity; uses user-defined helper.
    public static OfferDefinition ToOfferDefinition(CheckoutOfferEntity offer) =>
        new(
            offer.Code,
            MapSku(offer.Sku),
            MapOfferType(offer.Type),
            MapOfferState(offer.State),
            MapConfiguration(offer));

    // Offers require deterministic ordering by code before projection; user-defined to preserve that.
    [MapperIgnoreSource(nameof(CheckoutProductEntity.IsActive))]
    public static partial CheckoutCatalogItem ToCatalogItem(CheckoutProductEntity product);

    [MapperIgnoreSource(nameof(CheckoutOfferEntity.Product))]
    public static partial CheckoutOfferItem ToOfferItem(CheckoutOfferEntity offer);

    private static Sku MapSku(string value) => Sku.From(value);

    private static OfferType MapOfferType(int value) => (OfferType)value;

    private static OfferState MapOfferState(int value) => (OfferState)value;

    private static Money MapMoney(decimal amount) => CheckoutMoney.Pounds(amount);

    private static QuantityForFixedPriceConfiguration MapConfiguration(CheckoutOfferEntity offer) =>
        new(offer.Quantity, MapMoney(offer.FixedPriceAmount));

    private static IReadOnlyList<CheckoutOfferItem> MapOffers(ICollection<CheckoutOfferEntity> offers) =>
        [.. offers.OrderBy(o => o.Code, StringComparer.Ordinal).Select(ToOfferItem)];
}
