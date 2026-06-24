using BrightPay.TakeHome.Core.Checkout.Identifiers;
using BrightPay.TakeHome.Core.Checkout.Offers.Definitions;
using BrightPay.TakeHome.Core.Checkout.Offers.QuantityForFixedPrice;
using BrightPay.TakeHome.Core.Checkout.Pricing;
using BrightPay.TakeHome.Web.Data.Checkout;
using Riok.Mapperly.Abstractions;

namespace BrightPay.TakeHome.Web.Features.Checkout;

[Mapper]
internal static partial class CheckoutCatalogMapper
{
    public static ProductPrice ToProductPrice(CheckoutProductEntity product) =>
        new(Sku.From(product.Sku), CheckoutMoney.Pounds(product.UnitPriceAmount));

    public static OfferDefinition ToOfferDefinition(CheckoutOfferEntity offer) =>
        new(
            offer.Code,
            Sku.From(offer.Sku),
            (OfferType)offer.Type,
            (OfferState)offer.State,
            new QuantityForFixedPriceConfiguration(
                offer.Quantity,
                CheckoutMoney.Pounds(offer.FixedPriceAmount)));

    public static CheckoutCatalogItem ToCatalogItem(CheckoutProductEntity product) =>
        new(
            Sku.From(product.Sku),
            product.UnitPriceAmount,
            [
                .. product.Offers
                    .OrderBy(offer => offer.Code, StringComparer.Ordinal)
                    .Select(ToOfferItem),
            ]);

    [MapperIgnoreSource(nameof(CheckoutOfferEntity.Product))]
    public static partial CheckoutOfferItem ToOfferItem(CheckoutOfferEntity offer);

    private static Sku MapSku(string value) => Sku.From(value);

    private static OfferType MapOfferType(int value) => (OfferType)value;

    private static OfferState MapOfferState(int value) => (OfferState)value;
}
