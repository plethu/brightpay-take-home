using System.Globalization;
using BrightPay.TakeHome.Web.Components;
using BrightPay.TakeHome.Web.Data.Checkout;
using BrightPay.TakeHome.Web.Features.Checkout;
using BrightPay.TakeHome.Web.Features.Checkout.Projection;
using BrightPay.TakeHome.Web.Features.Checkout.Routing;
using BrightPay.TakeHome.Web.Features.Checkout.State;
using Microsoft.EntityFrameworkCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// Cultures the UI is translated for. The first entry is the default.
string[] supportedCultures = ["en-GB"];

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// App.razor resolves the theme cookie to server-render data-theme without flicker.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddScoped<CheckoutBasketCookieStore>();
builder.Services.AddScoped<CheckoutViewProjector>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string? checkoutConnection = builder.Configuration.GetConnectionString("CheckoutDatabase");
if (!string.IsNullOrWhiteSpace(checkoutConnection))
{
    builder.Services.AddDbContext<CheckoutDbContext>(options => options.UseSqlServer(checkoutConnection));
    // Keep offer evaluator registrations manual until the list grows; evaluate Scrutor for autodiscovery later.
    builder.Services.AddScoped<ICheckoutCatalogService, CheckoutCatalogService>();
}

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseSerilogRequestLogging();

if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapCheckoutEndpoints();

if (!string.IsNullOrWhiteSpace(checkoutConnection))
{
    AsyncServiceScope scope = app.Services.CreateAsyncScope();
    try
    {
        CheckoutDbContext dbContext = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }
    finally
    {
        await scope.DisposeAsync().ConfigureAwait(false);
    }
}

await app.RunAsync().ConfigureAwait(false);
