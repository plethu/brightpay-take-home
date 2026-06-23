using System.Text.RegularExpressions;

namespace BrightPay.TakeHome.Tooling;

internal static partial class ToolingRegexes
{
    [GeneratedRegex(@"(?m)^\s*dotnet\s*=\s*[""'](?<version>[^""']+)[""']\s*$", RegexOptions.None, 1_000)]
    public static partial Regex MiseDotnetPinRegex { get; }

    [GeneratedRegex(@"(?m)^\s*ARG\s+DOTNET_SDK_VERSION\s*=\s*(?<version>[^\s#]+)\s*$", RegexOptions.None, 1_000)]
    public static partial Regex DockerfileE2EDotnetSdkVersionRegex { get; }
}
