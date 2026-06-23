using System.Text.Json;
using System.Text.RegularExpressions;

using BrightPay.TakeHome.Tooling;

string command = args.Length > 0 ? args[0] : "help";

return command switch
{
    "check-toolchain" => CheckToolchain(),
    _ => Usage(),
};

static int CheckToolchain()
{
    string root = FindRepositoryRoot();
    ToolchainVersionPin[] dotnetSdkPins =
    [
        new(".mise.toml", ReadMiseDotnetVersion(Path.Combine(root, ".mise.toml"))),
        new("global.json", ReadGlobalJsonDotnetVersion(Path.Combine(root, "global.json"))),
        new("compose.yaml SDK image", ReadDotnetSdkImageVersion(Path.Combine(root, "compose.yaml"))),
        new(".env.example SDK image", ReadDotnetSdkImageVersion(Path.Combine(root, ".env.example"))),
    ];

    string expectedDotnetSdkVersion = dotnetSdkPins[0].Version;

    if (dotnetSdkPins.Any(pin => !StringComparer.Ordinal.Equals(pin.Version, expectedDotnetSdkVersion)))
    {
        Console.Error.WriteLine(".NET SDK version mismatch:");
        foreach (ToolchainVersionPin pin in dotnetSdkPins)
        {
            Console.Error.WriteLine($"  {pin.Source}: {pin.Version}");
        }

        return 1;
    }

    Console.WriteLine(
        $".NET SDK pin OK: {expectedDotnetSdkVersion} ({string.Join(", ", dotnetSdkPins.Select(pin => pin.Source))})"
    );
    return 0;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/BrightPay.TakeHome.Tooling -- check-toolchain");
    return 64;
}

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        if (
            File.Exists(Path.Combine(directory.FullName, ".mise.toml"))
            && File.Exists(Path.Combine(directory.FullName, "global.json"))
        )
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not find repository root containing .mise.toml and global.json.");
}

static string ReadMiseDotnetVersion(string path)
{
    string text = File.ReadAllText(path);
    Match match = ToolingRegexes.MiseDotnetPinRegex.Match(text);

    return match.Success
        ? match.Groups["version"].Value
        : throw new InvalidOperationException($"Could not find a dotnet tool pin in {path}.");
}

static string ReadGlobalJsonDotnetVersion(string path)
{
    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

    return document.RootElement.TryGetProperty("sdk", out JsonElement sdk)
        && sdk.TryGetProperty("version", out JsonElement version)
        && version.GetString() is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Could not find sdk.version in {path}.");
}

static string ReadDotnetSdkImageVersion(string path)
{
    string text = File.ReadAllText(path);
    Match match = ToolingRegexes.DotnetSdkImageVersionRegex.Match(text);

    return match.Success
        ? match.Groups["version"].Value
        : throw new InvalidOperationException($"Could not find a pinned .NET SDK image in {path}.");
}
