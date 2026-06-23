using System.Text.RegularExpressions;

namespace BrightPay.TakeHome.Tooling;

internal static partial class ToolingRegexes
{
    [GeneratedRegex(@"(?m)^\s*dotnet\s*=\s*[""'](?<version>[^""']+)[""']\s*$", RegexOptions.None, 1_000)]
    public static partial Regex MiseDotnetPinRegex { get; }

    [GeneratedRegex(@"mcr\.microsoft\.com/dotnet/sdk:(?<version>\d+\.\d+\.\d+)", RegexOptions.None, 1_000)]
    public static partial Regex DotnetSdkImageVersionRegex { get; }
}
