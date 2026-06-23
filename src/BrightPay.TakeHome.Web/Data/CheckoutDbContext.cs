using Microsoft.EntityFrameworkCore;

namespace BrightPay.TakeHome.Web.Data;

/// <summary>
/// SQL Server-backed catalogue context. Entities, configurations, and seed data
/// are added per deliverable (D1: Product and Offer); the shell exists so EF
/// Core tooling and dependency injection are wired for development from the start.
/// </summary>
public sealed class CheckoutDbContext(DbContextOptions<CheckoutDbContext> options) : DbContext(options);
