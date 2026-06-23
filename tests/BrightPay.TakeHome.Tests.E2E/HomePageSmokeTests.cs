using Microsoft.Playwright;

namespace BrightPay.TakeHome.Tests.E2E;

public sealed class HomePageSmokeTests : PageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task HomePageShowsReadinessDashboard()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Set E2E_BASE_URL to run browser smoke tests against a running app.");
        }

        _ = await Page.GotoAsync(baseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "BrightPay take-home" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByText("Current State")).ToBeVisibleAsync();
    }
}
