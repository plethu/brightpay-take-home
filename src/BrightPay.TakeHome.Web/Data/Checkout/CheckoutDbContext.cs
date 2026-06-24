using Microsoft.EntityFrameworkCore;

namespace BrightPay.TakeHome.Web.Data.Checkout;

public sealed class CheckoutDbContext(DbContextOptions<CheckoutDbContext> options) : DbContext(options)
{
    public DbSet<CheckoutProductEntity> Products => Set<CheckoutProductEntity>();

    public DbSet<CheckoutOfferEntity> Offers => Set<CheckoutOfferEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CheckoutProductEntity>(entity =>
        {
            entity.ToTable("CheckoutProducts");
            entity.HasKey(product => product.Sku);
            entity.Property(product => product.Sku).HasMaxLength(1).IsUnicode(false);
            entity.Property(product => product.UnitPriceAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(product => product.IsActive);
            entity.HasData(
                new CheckoutProductEntity { Sku = "A", UnitPriceAmount = 50m, IsActive = true },
                new CheckoutProductEntity { Sku = "B", UnitPriceAmount = 30m, IsActive = true },
                new CheckoutProductEntity { Sku = "C", UnitPriceAmount = 20m, IsActive = true },
                new CheckoutProductEntity { Sku = "D", UnitPriceAmount = 15m, IsActive = true });
        });

        modelBuilder.Entity<CheckoutOfferEntity>(entity =>
        {
            entity.ToTable("CheckoutOffers");
            entity.HasKey(offer => offer.Code);
            entity.Property(offer => offer.Code).HasMaxLength(64).IsUnicode(false);
            entity.Property(offer => offer.Sku).HasMaxLength(1).IsUnicode(false);
            entity.Property(offer => offer.FixedPriceAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(offer => new { offer.Sku, offer.State });
            entity
                .HasOne(offer => offer.Product)
                .WithMany(product => product.Offers)
                .HasForeignKey(offer => offer.Sku)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new CheckoutOfferEntity
                {
                    Code = "A-3-FOR-130",
                    Sku = "A",
                    Type = 1,
                    State = 1,
                    Quantity = 3,
                    FixedPriceAmount = 130m,
                },
                new CheckoutOfferEntity
                {
                    Code = "B-2-FOR-45",
                    Sku = "B",
                    Type = 1,
                    State = 1,
                    Quantity = 2,
                    FixedPriceAmount = 45m,
                });
        });
    }
}
