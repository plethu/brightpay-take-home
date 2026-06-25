using System.Text.Json;
using System.Text.RegularExpressions;

using BrightPay.TakeHome.Tooling;

string command = args.Length > 0 ? args[0] : "help";

return command switch
{
    "check-toolchain" => CheckToolchain(),
    "dev-dashboard" => DevDashboard(),
    _ => Usage(),
};

static int CheckToolchain()
{
    string root = FindRepositoryRoot();
    IReadOnlyDictionary<string, string> miseTools = ReadMiseTools(Path.Combine(root, ".mise.toml"));

    string MisePin(string tool)
    {
        return miseTools.TryGetValue(tool, out string? version)
            ? version
            : throw new InvalidOperationException($"Could not find a '{tool}' pin in .mise.toml [tools].");
    }

    // Every place a version is independently declared. First pin in each group is the source of
    // truth; the rest must match. Extend this list to guard a new pin against drift.
    VersionGroup[] groups =
    [
        new(".NET SDK",
        [
            new(".mise.toml", MisePin("dotnet")),
            new("global.json", ReadGlobalJsonDotnetVersion(Path.Combine(root, "global.json"))),
            new("compose.yaml SDK image", ReadVersion(Path.Combine(root, "compose.yaml"), ToolingRegexes.DotnetSdkImageVersionRegex, ".NET SDK image tag")),
            new("Dockerfile SDK image", ReadVersion(Path.Combine(root, "Dockerfile"), ToolingRegexes.DotnetSdkImageVersionRegex, ".NET SDK image tag")),
        ]),
        new("pnpm",
        [
            new(".mise.toml", MisePin("pnpm")),
            new("package.json packageManager", ReadVersion(Path.Combine(root, "package.json"), ToolingRegexes.PnpmPackageManagerRegex, "pnpm packageManager pin")),
        ]),
        new("OpenTofu",
        [
            new(".mise.toml", MisePin("opentofu")),
            new(".github/workflows/ci.yml", ReadVersion(Path.Combine(root, ".github", "workflows", "ci.yml"), ToolingRegexes.TofuWorkflowVersionRegex, "tofu_version")),
            new(".github/workflows/release.yml", ReadVersion(Path.Combine(root, ".github", "workflows", "release.yml"), ToolingRegexes.TofuWorkflowVersionRegex, "tofu_version")),
        ]),
        new("Playwright",
        [
            new("Directory.Packages.props", ReadVersion(Path.Combine(root, "Directory.Packages.props"), ToolingRegexes.PlaywrightPackageVersionRegex, "Microsoft.Playwright package version")),
            new("compose.yaml e2e image", ReadVersion(Path.Combine(root, "compose.yaml"), ToolingRegexes.PlaywrightImageVersionRegex, "Playwright image tag")),
        ]),
    ];

    bool allMatch = true;
    foreach (VersionGroup group in groups)
    {
        string expected = group.Pins[0].Version;
        if (group.Pins.Any(pin => !StringComparer.Ordinal.Equals(pin.Version, expected)))
        {
            allMatch = false;
            Console.Error.WriteLine($"{group.Name} version mismatch (expected {expected} from {group.Pins[0].Source}):");
            foreach (ToolchainVersionPin pin in group.Pins)
            {
                Console.Error.WriteLine($"  {pin.Source}: {pin.Version}");
            }
        }
        else
        {
            Console.WriteLine($"{group.Name} pin OK: {expected} ({string.Join(", ", group.Pins.Select(pin => pin.Source))})");
        }
    }

    return allMatch ? 0 : 1;
}

static int DevDashboard()
{
    string appPort = Environment.GetEnvironmentVariable("APP_HOST_PORT") ?? "8080";
    string sqlPort = Environment.GetEnvironmentVariable("SQL_HOST_PORT") ?? "14333";
    string webContainer = Environment.GetEnvironmentVariable("DEV_CONTAINER_NAME") ?? "brightpay-takehome-web";
    string dbContainer = Environment.GetEnvironmentVariable("DB_CONTAINER_NAME") ?? "brightpay-takehome-db";
    string baseUrl = $"http://localhost:{appPort}";
    string cartUrl = $"{baseUrl}/cart";

    if (!WaitForApp(cartUrl).GetAwaiter().GetResult())
    {
        Console.Error.WriteLine($"Timed out waiting for {cartUrl}.");
        Console.Error.WriteLine("Run `just logs` to inspect the web container.");
        return 1;
    }

    ClearConsole();
    WriteBrightLogo();

    Console.WriteLine($"Open: {baseUrl}/");
    Console.WriteLine();
    Console.WriteLine("Services");
    Console.WriteLine($"  Web:        {webContainer}");
    Console.WriteLine($"  SQL Server: 127.0.0.1:{sqlPort} ({dbContainer})");
    Console.WriteLine();
    Console.WriteLine("Commands");
    Console.WriteLine("  just logs       follow web logs");
    Console.WriteLine("  just shell-web  open a shell in the web container");
    Console.WriteLine("  just shell-db   open a shell in the SQL Server container");
    Console.WriteLine("  just db-update  apply EF Core migrations");
    Console.WriteLine("  just ps         show container status");
    Console.WriteLine("  just down       stop containers");

    return 0;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/BrightPay.TakeHome.Tooling -- <check-toolchain|dev-dashboard>");
    return 64;
}

static async Task<bool> WaitForApp(string url)
{
    using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(2) };
    DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(90);

    while (DateTimeOffset.UtcNow < deadline)
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri(url)).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
    }

    return false;
}

static void ClearConsole()
{
    try
    {
        Console.Clear();
    }
    catch (IOException)
    {
        Console.WriteLine();
    }
}

static void WriteBrightLogo()
{
    string[] logo =
    [
        "  .:-.",
        "-====.                                                       .",
        "=====.             .#######*+:          .*#*.             -+#=         :--",
        "=====.  .          .%%%*==*%%%- ....  ...+*+.   .... ... .%%%= ....  .#%%+",
        "=====.  ::...      .%%%+::=%%*: *##+=#%+:***: -*#%%#####=.%%%**%%##*..%%%##*",
        "=====.  ::::::.    .%%%%%%%%%+. *%%%%+=-:%%%--%%%=--=%%%+.%%%%+-=%%%*.%%%*--",
        "=====.   .:::::    .%%%=  .*%%#.*%%#.   :%%%-+%##.   %%%+.%%%+   #%#*.%%%=",
        "=====...:----::    .%%%*==+#%%# *%%#    -%%%-.#%%#**#%%%+.%%%=   #%%* #%%#+=",
        "====---======-.    .#######*+-  +##*    :###:  -=+*+=%%%+.###-   *##+ .=*###",
        ".:--====--:.                                  .+=---*%%#:",
        "    .:::.                                     :*##%%#+-.",
    ];

    foreach (string line in logo)
    {
        Console.WriteLine(line);
    }

    Console.WriteLine();
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

// Parse the [tools] table of .mise.toml (the source of truth for host tool versions) into a map.
static IReadOnlyDictionary<string, string> ReadMiseTools(string path)
{
    Dictionary<string, string> tools = new(StringComparer.Ordinal);
    bool inToolsTable = false;

    foreach (string rawLine in File.ReadLines(path))
    {
        string line = rawLine.Trim();
        if (line.StartsWith('['))
        {
            inToolsTable = string.Equals(line, "[tools]", StringComparison.Ordinal);
            continue;
        }

        if (!inToolsTable || line.Length == 0 || line.StartsWith('#'))
        {
            continue;
        }

        Match match = ToolingRegexes.TomlKeyValueRegex.Match(line);
        if (match.Success)
        {
            tools[match.Groups["key"].Value] = match.Groups["version"].Value;
        }
    }

    return tools;
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

static string ReadVersion(string path, Regex regex, string description)
{
    Match match = regex.Match(File.ReadAllText(path));

    return match.Success
        ? match.Groups["version"].Value
        : throw new InvalidOperationException($"Could not find {description} in {path}.");
}
