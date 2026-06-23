using BrightPay.TakeHome.Web.Components;
using BrightPay.TakeHome.Web.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Cultures the UI is translated for. The first entry is the default.
string[] supportedCultures = ["en-GB"];

_ = builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
_ = builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    _ = options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// App.razor resolves the theme cookie to server-render data-theme without flicker.
_ = builder.Services.AddHttpContextAccessor();

_ = builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQL Server catalogue (D1). Registered when a connection string is configured so
// the shell still runs standalone; design-time tooling uses CheckoutDbContextFactory.
string? checkoutConnection = builder.Configuration.GetConnectionString("CheckoutDatabase");
if (!string.IsNullOrWhiteSpace(checkoutConnection))
{
    _ = builder.Services.AddDbContext<CheckoutDbContext>(options => options.UseSqlServer(checkoutConnection));
}

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/error", createScopeForErrors: true);
    _ = app.UseHsts();
}

_ = app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    _ = app.UseHttpsRedirection();
}

_ = app.UseRequestLocalization();
_ = app.UseAntiforgery();

_ = app.MapStaticAssets();
_ = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);
