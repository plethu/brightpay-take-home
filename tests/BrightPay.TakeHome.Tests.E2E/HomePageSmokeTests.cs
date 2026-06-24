using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace BrightPay.TakeHome.Tests.E2E;

public sealed class HomePageSmokeTests : PageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ShellRendersLocalizedHomePage()
    {
        string baseUrl = RequireBaseUrl();

        await Page.GotoAsync(baseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "BrightPay Checkout", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Skip to main content" }))
            .ToBeAttachedAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task ShellHasNoBlockingAccessibilityViolations()
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
}
