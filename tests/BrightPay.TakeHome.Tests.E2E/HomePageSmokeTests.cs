using System.Text.RegularExpressions;
using System.Globalization;
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

        await Page.GotoAsync(baseUrl).ConfigureAwait(true);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = Text("CheckoutAddHeading"), Exact = true }))
            .ToBeVisibleAsync().ConfigureAwait(true);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = Text("CheckoutCurrentSale"), Exact = true }))
            .ToBeVisibleAsync().ConfigureAwait(true);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = Text("ShellSkipToContent") }))
            .ToBeAttachedAsync().ConfigureAwait(true);

        await AssertSkuPadPrecedesManualEntryAsync(Page).ConfigureAwait(true);
        await AssertScanButtonAlignsWithInputAsync(Page, Text("CheckoutScanLabel"), Text("CheckoutAddButton")).ConfigureAwait(true);
        await Expect(Page.Locator("[data-checkout-page][data-interactive]")).ToBeAttachedAsync().ConfigureAwait(true);
        await AddSkuAsync(Page, "A", 3).ConfigureAwait(true);
        await AssertLineQuantityControlAlignedAsync(Page, "A", Text("SkuName_A")).ConfigureAwait(true);
        await AssertCheckoutActionsLayoutAsync(Page).ConfigureAwait(true);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_AddedQuantity", 3, Text("SkuName_A"))).ConfigureAwait(true);
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30").ConfigureAwait(true);
        await Expect(Page.Locator(".sale-pane").GetByText(Text("CheckoutOffer_QuantityForFixedPrice", 3, "£1.30"))).ToBeVisibleAsync().ConfigureAwait(true);

        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutDecreaseLineLabel", Text("SkuName_A")) }).ClickAsync().ConfigureAwait(true);
        await Expect(Page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.00").ConfigureAwait(true);

        await Page.GetByLabel(Text("CheckoutScanLabel")).FillAsync("Z").ConfigureAwait(true);
        await Page.GetByLabel(Text("CheckoutScanLabel")).PressAsync("Enter").ConfigureAwait(true);
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync(Text("CheckoutError_UnknownSku", "Z")).ConfigureAwait(true);
        await Expect(Page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1).ConfigureAwait(true);

        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClear") }).ClickAsync().ConfigureAwait(true);
        await Page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClearConfirmAction") }).ClickAsync().ConfigureAwait(true);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Cleared")).ConfigureAwait(true);

        await AddSkuAsync(Page, "B", 2).ConfigureAwait(true);
        await Page.Locator("[data-action='checkout']").ClickAsync().ConfigureAwait(true);
        await Expect(Page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Charged")).ConfigureAwait(true);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutMicroAnimationsAreAttachedToInteractiveUpdates()
    {
        string baseUrl = RequireBaseUrl();

        await Page.GotoAsync(baseUrl).ConfigureAwait(true);

        await AddSkuAsync(Page, "A", 1).ConfigureAwait(true);
        await ExpectAnimationAsync(Page.Locator(".toast"), "checkout-toast-in").ConfigureAwait(true);
        await ExpectAnimationAsync(Page.Locator(".line-qty"), "qty-pulse").ConfigureAwait(true);
        await ExpectAnimationAsync(Page.Locator(".line-price"), "price-pulse").ConfigureAwait(true);
        await ExpectAnimationAsync(Page.Locator(".checkout-amount"), "amount-pulse").ConfigureAwait(true);

        await AddSkuAsync(Page, "A", 2).ConfigureAwait(true);
        await ExpectAnimationAsync(Page.Locator(".sale-line[data-sku='A'] .line-meta"), "offer-in").ConfigureAwait(true);

        await Page.GetByLabel(Text("CheckoutScanLabel")).FillAsync("Z").ConfigureAwait(true);
        await Page.GetByLabel(Text("CheckoutScanLabel")).PressAsync("Enter").ConfigureAwait(true);
        await ExpectAnimationAsync(Page.GetByRole(AriaRole.Alert), "checkout-error-in").ConfigureAwait(true);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CheckoutNoJsPathSupportsServerRenderedMutations()
    {
        string baseUrl = RequireBaseUrl();

        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            JavaScriptEnabled = false,
            ReducedMotion = ReducedMotion.Reduce,
        }).ConfigureAwait(true);
        try
        {
            IPage page = await context.NewPageAsync().ConfigureAwait(true);

            await page.GotoAsync(baseUrl).ConfigureAwait(true);
            await AddSkuAsync(page, "A", 3).ConfigureAwait(true);
            await Expect(page.Locator(".toast")).ToContainTextAsync(Text("CheckoutToast_Added", Text("SkuName_A"))).ConfigureAwait(true);
            await Expect(page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.30").ConfigureAwait(true);

            await page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutDecreaseLineLabel", Text("SkuName_A")) }).ClickAsync().ConfigureAwait(true);
            await Expect(page.Locator("[data-action='checkout']")).ToContainTextAsync("£1.00").ConfigureAwait(true);

            await page.GetByLabel(Text("CheckoutScanLabel")).FillAsync("Z").ConfigureAwait(true);
            await page.GetByLabel(Text("CheckoutScanLabel")).PressAsync("Enter").ConfigureAwait(true);
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync(Text("CheckoutError_UnknownSku", "Z")).ConfigureAwait(true);
            await Expect(page.Locator(".sale-line[data-sku='A']")).ToHaveCountAsync(1).ConfigureAwait(true);

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

    private Task ExpectAnimationAsync(ILocator locator, string animationNamePrefix)
    {
        Regex scopedAnimationName = new(
            $"^{Regex.Escape(animationNamePrefix)}(?:-[a-z0-9]+)*$",
            RegexOptions.None,
            TimeSpan.FromMilliseconds(100));
        return Expect(locator).ToHaveCSSAsync("animation-name", scopedAnimationName);
    }

    private async Task AssertScanButtonAlignsWithInputAsync(IPage page, string scanLabel, string addLabel)
    {
        ILocator input = page.GetByLabel(scanLabel);
        ILocator button = page.GetByRole(AriaRole.Button, new() { Name = addLabel, Exact = true });

        await Expect(input).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(button).ToBeVisibleAsync().ConfigureAwait(false);
        await input.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);

        LocatorBoundingBoxResult inputBox = await input.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The manual SKU input should have a layout box.");
        LocatorBoundingBoxResult buttonBox = await button.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The manual SKU add button should have a layout box.");

        Assert.InRange(Math.Abs(inputBox.Y - buttonBox.Y), 0, 2);
        Assert.InRange(Math.Abs(inputBox.Y + inputBox.Height - buttonBox.Y - buttonBox.Height), 0, 2);
    }

    private async Task AssertSkuPadPrecedesManualEntryAsync(IPage page)
    {
        ILocator skuPad = page.Locator(".sku-pad");
        ILocator manualEntry = page.Locator(".manual");

        await Expect(skuPad).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(manualEntry).ToBeVisibleAsync().ConfigureAwait(false);

        LocatorBoundingBoxResult skuPadBox = await skuPad.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The SKU pad should have a layout box.");
        LocatorBoundingBoxResult manualEntryBox = await manualEntry.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The manual SKU entry should have a layout box.");

        Assert.True(
            skuPadBox.Y < manualEntryBox.Y,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The SKU pad should render before manual entry. SKU pad Y={skuPadBox.Y}, manual entry Y={manualEntryBox.Y}."));
    }

    private async Task AssertLineQuantityControlAlignedAsync(IPage page, string sku, string skuName)
    {
        ILocator decrement = page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutDecreaseLineLabel", skuName) });
        ILocator quantity = page.Locator($".sale-line[data-sku='{sku}'] .line-qty");
        ILocator increment = page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutAddOneLabel", skuName) });

        await Expect(decrement).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(quantity).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(increment).ToBeVisibleAsync().ConfigureAwait(false);

        LocatorBoundingBoxResult decrementBox = await decrement.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The decrement button should have a layout box.");
        LocatorBoundingBoxResult quantityBox = await quantity.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The line quantity should have a layout box.");
        LocatorBoundingBoxResult incrementBox = await increment.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The increment button should have a layout box.");

        double controlCenter = CenterY(quantityBox);
        Assert.InRange(Math.Abs(CenterY(decrementBox) - controlCenter), 0, 2);
        Assert.InRange(Math.Abs(CenterY(incrementBox) - controlCenter), 0, 2);
    }

    private static double CenterY(LocatorBoundingBoxResult box) => box.Y + (box.Height / 2);

    private async Task AssertCheckoutActionsLayoutAsync(IPage page)
    {
        ILocator clearButton = page.GetByRole(AriaRole.Button, new() { Name = Text("CheckoutClear"), Exact = true });
        ILocator actionRow = page.Locator(".total-actions");
        ILocator chargeButton = page.Locator("[data-action='checkout']");
        ILocator chargeLabel = chargeButton.Locator(".checkout-label");
        ILocator chargeAmount = chargeButton.Locator(".checkout-amount");

        await Expect(actionRow).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(clearButton).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(chargeButton).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(chargeLabel).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(chargeAmount).ToBeVisibleAsync().ConfigureAwait(false);

        LocatorBoundingBoxResult actionRowBox = await actionRow.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The checkout action row should have a layout box.");
        LocatorBoundingBoxResult clearBox = await clearButton.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The clear button should have a layout box.");
        LocatorBoundingBoxResult chargeBox = await chargeButton.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The charge button should have a layout box.");
        LocatorBoundingBoxResult chargeLabelBox = await chargeLabel.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The charge label should have a layout box.");
        LocatorBoundingBoxResult chargeAmountBox = await chargeAmount.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The charge amount should have a layout box.");

        Assert.True(
            chargeBox.Width > clearBox.Width * 2.5,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The charge action should be wider than clear. Clear width={clearBox.Width}, charge width={chargeBox.Width}."));
        Assert.InRange(Math.Abs(chargeBox.Height - clearBox.Height), 0, 2);
        double actionRowEnd = actionRowBox.X + actionRowBox.Width;
        double chargeEnd = chargeBox.X + chargeBox.Width;
        Assert.InRange(Math.Abs(chargeEnd - actionRowEnd), 0, 2);
        Assert.True(
            chargeAmountBox.X > chargeLabelBox.X,
            "The charge amount should render after the charge label.");
    }

}
