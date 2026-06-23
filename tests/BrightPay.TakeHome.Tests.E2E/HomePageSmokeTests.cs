using Microsoft.Playwright;

namespace BrightPay.TakeHome.Tests.E2E;

public sealed class HomePageSmokeTests : PageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ShellRendersLocalizedHomePage()
    {
        string baseUrl = RequireBaseUrl();

        _ = await Page.GotoAsync(baseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "BrightPay Checkout", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Skip to main content" }))
            .ToBeAttachedAsync();
    }

    private static string RequireBaseUrl()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

        return string.IsNullOrWhiteSpace(baseUrl)
            ? throw new InvalidOperationException(
                "Set E2E_BASE_URL to run browser smoke tests against a running app.")
            : baseUrl;
    }
}
