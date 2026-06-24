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
    ToolchainVersionPin[] dotnetSdkPins =
    [
        new(".mise.toml", ReadMiseDotnetVersion(Path.Combine(root, ".mise.toml"))),
        new("global.json", ReadGlobalJsonDotnetVersion(Path.Combine(root, "global.json"))),
        new("compose.yaml SDK image", ReadDotnetSdkImageVersion(Path.Combine(root, "compose.yaml"))),
        new("Dockerfile SDK image", ReadDotnetSdkImageVersion(Path.Combine(root, "Dockerfile"))),
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

    Console.WriteLine("BrightPay Take-Home is running");
    Console.WriteLine();
    Console.WriteLine("App");
    Console.WriteLine($"  Cart:       {cartUrl}");
    Console.WriteLine($"  Home:       {baseUrl}/");
    Console.WriteLine();
    Console.WriteLine("Services");
    Console.WriteLine($"  Web:        {webContainer}");
    Console.WriteLine($"  SQL Server: 127.0.0.1:{sqlPort} ({dbContainer})");
    Console.WriteLine();
    Console.WriteLine("Development commands");
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
