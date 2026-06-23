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
    string misePath = Path.Combine(root, ".mise.toml");
    string globalJsonPath = Path.Combine(root, "global.json");

    string miseDotnet = ReadMiseDotnetVersion(misePath);
    string globalJsonDotnet = ReadGlobalJsonDotnetVersion(globalJsonPath);

    if (!StringComparer.Ordinal.Equals(miseDotnet, globalJsonDotnet))
    {
        Console.Error.WriteLine(
            $".NET SDK version mismatch: .mise.toml pins {miseDotnet}, but global.json pins {globalJsonDotnet}."
        );
        return 1;
    }

    Console.WriteLine($".NET SDK pin OK: {globalJsonDotnet}");
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
