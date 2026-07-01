using AwesomeAssertions;
using BrightPay.TakeHome.Web.Data.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class CheckoutCatalogModelTests
{
    [Fact]
    public void ModelSeedsSpecProductsAndOffers()
    {
        DbContextOptions<CheckoutDbContext> options = new DbContextOptionsBuilder<CheckoutDbContext>()
            .UseSqlServer("Server=(local);Database=BrightPayTakeHomeTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using CheckoutDbContext dbContext = new(options);

        IModel model = dbContext.GetService<IDesignTimeModel>().Model;
        IEntityType productType = model.FindEntityType(typeof(CheckoutProductEntity))
            ?? throw new InvalidOperationException("Product entity is not configured.");
        IEntityType offerType = model.FindEntityType(typeof(CheckoutOfferEntity))
            ?? throw new InvalidOperationException("Offer entity is not configured.");

        IReadOnlyList<IDictionary<string, object?>> products = [.. productType.GetSeedData()];
        IReadOnlyList<IDictionary<string, object?>> offers = [.. offerType.GetSeedData()];

        products.Select(product => product["Sku"]).Should().Equal("A", "B", "C", "D");
        products.Select(product => product["UnitPriceAmount"]).Should().Equal(50m, 30m, 20m, 15m);
        offers.Select(offer => offer["Code"]).Should().Equal("A-3-FOR-130", "B-2-FOR-45");
        offers.Select(offer => offer["ConfigurationVersion"]).Should().Equal(1, 1);
        offers.Select(ConfigurationQuantity).Should().Equal(3, 2);
        offers.Select(ConfigurationMinorUnits).Should().Equal(130m, 45m);
    }

    private static int ConfigurationQuantity(IDictionary<string, object?> offer) =>
        JsonDocument.Parse((string)offer["ConfigurationJson"]!).RootElement.GetProperty("quantity").GetInt32();

    private static decimal ConfigurationMinorUnits(IDictionary<string, object?> offer) =>
        JsonDocument.Parse((string)offer["ConfigurationJson"]!)
            .RootElement
            .GetProperty("fixedPrice")
            .GetProperty("minorUnits")
            .GetDecimal();
}
