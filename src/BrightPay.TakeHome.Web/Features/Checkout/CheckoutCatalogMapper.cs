using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions ConfigurationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

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
            MapConfiguration(offer),
            MapOfferScope(offer.Scope),
            offer.Priority,
            MapOfferCombinationRule(offer.CombinationRule));

    // Offers require deterministic ordering by code before projection; user-defined to preserve that.
    [MapperIgnoreSource(nameof(CheckoutProductEntity.IsActive))]
    public static partial CheckoutCatalogItem ToCatalogItem(CheckoutProductEntity product);

    public static CheckoutOfferItem ToOfferItem(CheckoutOfferEntity offer) =>
        new(
            offer.Code,
            MapSku(offer.Sku),
            MapOfferType(offer.Type),
            MapOfferState(offer.State),
            offer.ConfigurationVersion,
            MapConfiguration(offer));

    private static Sku MapSku(string value) => Sku.From(value);

    private static OfferType MapOfferType(int value) =>
        Enum.IsDefined((OfferType)value)
            ? (OfferType)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown offer type in persisted catalog.");

    private static OfferState MapOfferState(int value) =>
        Enum.IsDefined((OfferState)value)
            ? (OfferState)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown offer state in persisted catalog.");

    private static OfferScope MapOfferScope(int value) =>
        Enum.IsDefined((OfferScope)value)
            ? (OfferScope)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown offer scope in persisted catalog.");

    private static OfferCombinationRule MapOfferCombinationRule(int value) =>
        Enum.IsDefined((OfferCombinationRule)value)
            ? (OfferCombinationRule)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown offer combination rule in persisted catalog.");

    private static Money MapMoney(CheckoutMoneyConfigurationJson money)
    {
        return string.Equals(money.Currency, CheckoutMoney.CurrencyCode, StringComparison.Ordinal)
            ? CheckoutMoney.FromPence(money.MinorUnits)
            : throw new ArgumentException("Offer prices must be in GBP.", nameof(money));
    }

    private static QuantityForFixedPriceConfiguration MapConfiguration(CheckoutOfferEntity offer)
    {
        OfferType type = MapOfferType(offer.Type);
        return type switch
        {
            OfferType.QuantityForFixedPrice => MapQuantityForFixedPriceConfiguration(offer),
            OfferType.None => throw new InvalidOperationException("Offer type 'None' cannot be mapped to checkout configuration."),
            _ => throw new InvalidOperationException($"No checkout configuration mapper is registered for offer type '{type}'."),
        };
    }

    private static QuantityForFixedPriceConfiguration MapQuantityForFixedPriceConfiguration(CheckoutOfferEntity offer)
    {
        if (offer.ConfigurationVersion != 1)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Unsupported QuantityForFixedPrice configuration version '{offer.ConfigurationVersion}' for offer '{offer.Code}'."));
        }

        QuantityForFixedPriceConfigurationJson configuration = JsonSerializer.Deserialize<QuantityForFixedPriceConfigurationJson>(
                offer.ConfigurationJson,
                ConfigurationJsonOptions)
            ?? throw new JsonException($"Offer '{offer.Code}' has an empty configuration payload.");

        return new QuantityForFixedPriceConfiguration(
            configuration.Quantity,
            MapMoney(configuration.FixedPrice));
    }

    private static IReadOnlyList<CheckoutOfferItem> MapOffers(ICollection<CheckoutOfferEntity> offers) =>
        [.. offers.OrderBy(o => o.Code, StringComparer.Ordinal).Select(ToOfferItem)];

    private sealed record QuantityForFixedPriceConfigurationJson(
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("fixedPrice")] CheckoutMoneyConfigurationJson FixedPrice);

    private sealed record CheckoutMoneyConfigurationJson(
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("minorUnits")] decimal MinorUnits);
}
