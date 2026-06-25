namespace BrightPay.TakeHome.Tooling;

// A logical version (e.g. "pnpm") and every place it is independently declared. The first pin is
// the canonical source; the rest must match it. Add a group to guard a new version against drift.
internal sealed record VersionGroup(string Name, IReadOnlyList<ToolchainVersionPin> Pins);
