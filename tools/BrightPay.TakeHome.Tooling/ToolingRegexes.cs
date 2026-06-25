using System.Text.RegularExpressions;

namespace BrightPay.TakeHome.Tooling;

internal static partial class ToolingRegexes
{
    // A single `key = "value"` line inside a TOML table (e.g. .mise.toml [tools]).
    [GeneratedRegex(@"^(?<key>[A-Za-z0-9_.-]+)\s*=\s*[""'](?<version>[^""']+)[""']", RegexOptions.None, 1_000)]
    public static partial Regex TomlKeyValueRegex { get; }

    [GeneratedRegex(@"mcr\.microsoft\.com/dotnet/sdk:(?<version>\d+\.\d+\.\d+)", RegexOptions.None, 1_000)]
    public static partial Regex DotnetSdkImageVersionRegex { get; }

    [GeneratedRegex(@"""packageManager""\s*:\s*""pnpm@(?<version>[^""]+)""", RegexOptions.None, 1_000)]
    public static partial Regex PnpmPackageManagerRegex { get; }

    [GeneratedRegex(@"tofu_version:\s*(?<version>\d+\.\d+\.\d+)", RegexOptions.None, 1_000)]
    public static partial Regex TofuWorkflowVersionRegex { get; }

    [GeneratedRegex(@"Include=""Microsoft\.Playwright[^""]*""\s+Version=""(?<version>\d+\.\d+\.\d+)""", RegexOptions.None, 1_000)]
    public static partial Regex PlaywrightPackageVersionRegex { get; }

    [GeneratedRegex(@"mcr\.microsoft\.com/playwright:v(?<version>\d+\.\d+\.\d+)", RegexOptions.None, 1_000)]
    public static partial Regex PlaywrightImageVersionRegex { get; }
}
