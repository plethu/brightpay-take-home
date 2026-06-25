using BrightPay.TakeHome.Web;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BrightPay.TakeHome.Tests.E2E;

public sealed class HomePageSmokeTests : PageTest
{
    private readonly IStringLocalizer<SharedResource> _localizer = BuildLocalizer();

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutSupportsInteractiveSaleFlow()
    {
        string baseUrl = RequireBaseUrl();

        await Page.GotoAsync(baseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = Text("CheckoutAddHeading"), Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = Text("CheckoutCurrentSale"), Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = Text("ShellSkipToContent") }))
            .ToBeAttachedAsync();

        await AssertScanButtonAlignsWithInputAsync(Page, Text("CheckoutScanLabel"), Text("CheckoutAddButton"));
        await Expect(Page.Locator("[data-checkout-page][data-interactive]")).ToBeAttachedAsync();
        await AddSkuAsync(Page, "A", 3);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_AddedQuantity", 3, Text("SkuName_A")));
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30");
        await Expect(Page.Locator(".sale-pane").GetByText(Text("CheckoutOffer_QuantityForFixedPrice", 3, "£1.30"))).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutDecreaseLineLabel", Text("SkuName_A")) }).ClickAsync();
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.00");

        await Page.GetByLabel(Text("CheckoutScanLabel")).FillAsync("Z");
        await Page.GetByLabel(Text("CheckoutScanLabel")).PressAsync("Enter");
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync(Text("CheckoutError_UnknownSku", "Z"));
        await Expect(Page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1);

        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClear") }).ClickAsync().ConfigureAwait(true);
        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClearConfirmAction") }).ClickAsync().ConfigureAwait(true);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Cleared")).ConfigureAwait(true);

        await AddSkuAsync(Page, "B", 2).ConfigureAwait(true);
        await Page.Locator("[data-action='checkout']").ClickAsync().ConfigureAwait(true);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Charged")).ConfigureAwait(true);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutNoJsPathSupportsServerRenderedMutations()
    {
        string baseUrl = RequireBaseUrl();

        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            JavaScriptEnabled = false,
        }).ConfigureAwait(true);
        try
        {
            IPage page = await context.NewPageAsync().ConfigureAwait(true);

            await page.GotoAsync(baseUrl).ConfigureAwait(true);
            await AddSkuAsync(page, "A", 3).ConfigureAwait(true);
            await Expect(page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Added", Text("SkuName_A"))).ConfigureAwait(true);
            await Expect(page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30").ConfigureAwait(true);

            await page.GetByLabel(Text("CheckoutScanLabel")).FillAsync("Z").ConfigureAwait(true);
            await page.GetByLabel(Text("CheckoutScanLabel")).PressAsync("Enter").ConfigureAwait(true);
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync(Text("CheckoutError_UnknownSku", "Z")).ConfigureAwait(true);
            await Expect(page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1).ConfigureAwait(true);

            await page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutDecreaseLineLabel", Text("SkuName_A")) }).ClickAsync().ConfigureAwait(true);
            await Expect(page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.00").ConfigureAwait(true);

            await page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClear") }).ClickAsync().ConfigureAwait(true);
            await Expect(page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Cleared")).ConfigureAwait(true);
        }
        finally
        {
            await context.CloseAsync().ConfigureAwait(true);
        }
    }

    // Regression guard for the "interactive state not persisted" class: a basket built through the
    // interactive circuit must survive a full page reload, because the session cookie keys
    // server-side state and prerender rehydrates from it. Mutating-then-reloading is the standing
    // check, not a one-off.
    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutBasketSurvivesPageReload()
    {
        string baseUrl = RequireBaseUrl();

        await Page.GotoAsync(baseUrl);
        await AddSkuAsync(Page, "A", 3);
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30");
        await Expect(Page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1);

        await Page.ReloadAsync();

        await Expect(Page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30");
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutHasNoBlockingAccessibilityViolations()
    {
        string baseUrl = RequireBaseUrl();

        await Page.GotoAsync(baseUrl);

        AxeResult results = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions
            {
                Type = "tag",
                Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"],
            },
            ResultTypes = [ResultType.Violations],
        });

        AxeResultItem[] blockingViolations = [.. results.Violations.Where(IsBlockingViolation)];

        Assert.True(blockingViolations.Length == 0, FormatViolations(blockingViolations));
    }

    private static string RequireBaseUrl()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

        return string.IsNullOrWhiteSpace(baseUrl)
            ? throw new InvalidOperationException(
                "Set E2E_BASE_URL to run browser smoke tests against a running app.")
            : baseUrl;
    }

    private static string FormatViolations(IEnumerable<AxeResultItem> violations)
    {
        return string.Join(
            Environment.NewLine,
            violations.Select(violation =>
                $"{violation.Id} [{violation.Impact}]: {violation.Help} ({violation.HelpUrl})"));
    }

    private static bool IsBlockingViolation(AxeResultItem violation)
    {
        return string.Equals(violation.Impact, "serious", StringComparison.Ordinal)
            || string.Equals(violation.Impact, "critical", StringComparison.Ordinal);
    }

    private string Text(string key, params object[] arguments) => _localizer[key, arguments].Value;

    private static IStringLocalizer<SharedResource> BuildLocalizer()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
    }

    private static async Task AddSkuAsync(IPage page, string sku, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await page.Locator($".sku-tile[value='{sku}']").ClickAsync().ConfigureAwait(false);
        }
    }

    private static async Task AssertScanButtonAlignsWithInputAsync(IPage page, string scanLabel, string addLabel)
    {
        ILocator input = page.GetByLabel(scanLabel);
        ILocator button = page.GetByRole(AriaRole.Button, new() { Name = addLabel, Exact = true });

        LocatorBoundingBoxResult? inputBox = null;
        LocatorBoundingBoxResult? buttonBox = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await input.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                inputBox = await input.BoundingBoxAsync().ConfigureAwait(false);
                buttonBox = await button.BoundingBoxAsync().ConfigureAwait(false);
                if (inputBox is not null && buttonBox is not null)
                {
                    break;
                }
            }
            catch (PlaywrightException)
            {
                await page.WaitForTimeoutAsync(100).ConfigureAwait(false);
            }
        }

        if (inputBox is null)
        {
            throw new InvalidOperationException("The manual SKU input should be visible.");
        }

        if (buttonBox is null)
        {
            throw new InvalidOperationException("The manual SKU add button should be visible.");
        }

        Assert.InRange(Math.Abs(inputBox.Y - buttonBox.Y), 0, 2);
        Assert.InRange(Math.Abs(inputBox.Y + inputBox.Height - buttonBox.Y - buttonBox.Height), 0, 2);
    }
}
