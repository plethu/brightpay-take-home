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
            "Blazor Web App is ready for the take-home workflow once the spec arrives.",
            ReadinessStatus.Ready),
        new(
            "Domain model",
            "Core project exists so business rules can stay separate from Razor components.",
            ReadinessStatus.Ready),
        new(
            "Product requirements",
            "Replace this readiness screen with the requested workflow when BrightPay sends the spec.",
            ReadinessStatus.PendingSpec),
    ];

    public static int ReadyCount => Items.Count(static item => item.Status == ReadinessStatus.Ready);
}
