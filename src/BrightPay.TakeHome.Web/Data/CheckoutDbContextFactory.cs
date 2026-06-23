using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BrightPay.TakeHome.Web.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the context without booting
/// the host. Reads the same connection string the app uses at runtime.
/// </summary>
public sealed class CheckoutDbContextFactory : IDesignTimeDbContextFactory<CheckoutDbContext>
{
    public CheckoutDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CheckoutDatabase")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__CheckoutDatabase to run EF Core design-time commands.");

        DbContextOptions<CheckoutDbContext> options = new DbContextOptionsBuilder<CheckoutDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CheckoutDbContext(options);
    }
}
