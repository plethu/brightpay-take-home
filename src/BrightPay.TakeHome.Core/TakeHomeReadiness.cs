namespace BrightPay.TakeHome.Core;

public static class TakeHomeReadiness
{
    public static IReadOnlyList<ReadinessItem> Items { get; } =
    [
        new(
            "Toolchain",
            ".NET 10, mise, just, formatting, tests, and CI are scaffolded.",
            ReadinessStatus.Ready),
        new(
            "Application shell",
            "Blazor Web App is configured for server rendering with targeted client interactivity.",
            ReadinessStatus.Ready),
        new(
            "Domain model",
            "Core project is ready for checkout pricing rules and transaction state separate from Razor components.",
            ReadinessStatus.Ready),
        new(
            "Product requirements",
            "docs/SPEC.md defines the checkout kata: configurable pricing rules, item scanning, and repeatable offers.",
            ReadinessStatus.SpecLoaded),
    ];

    public static int ReadyCount => Items.Count(static item => item.Status == ReadinessStatus.Ready);
}
