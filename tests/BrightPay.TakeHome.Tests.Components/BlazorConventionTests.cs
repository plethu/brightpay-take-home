using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace BrightPay.TakeHome.Tests.Components;

/// <summary>
/// Executable documentation of the Blazor interaction idiom. Each test maps to a review finding
/// where an agent re-derived the architecture and reached for a hack; the failure message names
/// the idiomatic replacement so the build, not a reviewer, closes the loop. See
/// <c>.agents/skills/brightpay-blazor-frontend/SKILL.md</c> ("Anti-patterns — do not reintroduce").
///
/// These are deliberately string-scan heuristics over source files, not compiled behavior. They
/// stay cheap, run on every platform under <c>just test-components</c>, and are scoped to avoid
/// false positives on the legitimate patterns already in the tree (documented per test).
/// </summary>
public sealed partial class BlazorConventionTests
{
    private static readonly string[] InteractiveRenderModes =
        ["InteractiveServer", "InteractiveWebAssembly", "InteractiveAuto"];

    private static readonly string[] WebRoot =
        ["src", "BrightPay.TakeHome.Web"];

    private static readonly string[] ForbiddenJsDomTokens =
        ["MutationObserver", ".innerHTML", ".outerHTML", "requestSubmit", "document.querySelector"];

    /// <summary>
    /// Finding #1: an interactive component MUST NOT load state from HttpContext as if it were a
    /// controller. The legitimate prerender pattern reads HttpContext once and bridges the value
    /// across the circuit boundary with PersistentComponentState — so the rule is not "no
    /// HttpContext", it is "no HttpContext without the persistence bridge". A raw read with no
    /// bridge (the original CheckoutPage hack) fails here; the remediated CheckoutPage passes
    /// because it pairs the read with RegisterOnPersisting + TryTakeFromJson.
    /// </summary>
    [Fact]
    public void InteractiveComponentsBridgeHttpContextThroughPersistentState()
    {
        foreach (RazorFile razor in RazorFiles())
        {
            if (!IsInteractive(razor.Text) || !ReferencesHttpContext(razor.Text))
            {
                continue;
            }

            bool bridged = razor.Text.Contains("RegisterOnPersisting", StringComparison.Ordinal)
                && razor.Text.Contains("TryTakeFromJson", StringComparison.Ordinal);

            bridged.Should().BeTrue(
                $"{razor.RelativePath} is interactive and reads HttpContext, so it must flow that "
                + "prerender state across the circuit boundary with PersistentComponentState "
                + "(RegisterOnPersisting + TryTakeFromJson) instead of reading HttpContext as "
                + "interactive component state.");
        }
    }

    /// <summary>
    /// Finding #2: in interactive render modes Blazor owns the DOM. Collocated JS modules MUST NOT
    /// mutate or observe the DOM Blazor renders. Enter/leave/pulse come from component state + CSS.
    /// </summary>
    [Fact]
    public void RazorJsModulesDoNotMutateOrObserveTheDom()
    {
        foreach (RazorJsFile module in RazorJsFiles())
        {
            string[] hits = [.. ForbiddenJsDomTokens.Where(token =>
                module.Text.Contains(token, StringComparison.Ordinal))];

            hits.Should().BeEmpty(
                $"{module.RelativePath} uses {string.Join(", ", hits)}: Blazor owns the DOM in "
                + "interactive render modes. Drive enter/leave/pulse from component state and CSS "
                + "(@key remount, delayed removal), not manual DOM mutation or observation.");
        }
    }

    /// <summary>
    /// Finding #3: a JS module MUST NOT reference selectors that no rendered markup produces. Every
    /// data-* or class selector named in a .razor.js must exist in some .razor (the dead
    /// data-clear-dialog path is what this catches).
    /// </summary>
    [Fact]
    public void RazorJsSelectorsExistInMarkup()
    {
        string allMarkup = string.Concat(RazorFiles().Select(razor => razor.Text));

        foreach (RazorJsFile module in RazorJsFiles())
        {
            foreach (string selector in ReferencedSelectors(module.Text))
            {
                allMarkup.Should().Contain(
                    selector,
                    $"{module.RelativePath} references selector token \"{selector}\" but no .razor "
                    + "renders it. Delete the dead path or render the markup it targets.");
            }
        }
    }

    /// <summary>
    /// Finding #8: an EditForm with user-editable inputs MUST contain a DataAnnotationsValidator and
    /// surface messages (ValidationSummary or ValidationMessage). Scoped to input-bearing forms so
    /// the hidden-only clear/charge POST wrappers in CheckoutTotals do not false-positive.
    /// </summary>
    [Fact]
    public void InputBearingFormsDeclareValidation()
    {
        foreach (RazorFile razor in RazorFiles())
        {
            foreach (string form in EditFormBlocks(razor.Text))
            {
                if (!HasEditableInput(form))
                {
                    continue;
                }

                form.Contains("DataAnnotationsValidator", StringComparison.Ordinal).Should().BeTrue(
                    $"an EditForm in {razor.RelativePath} has user-editable inputs, so it must wire "
                    + "a DataAnnotationsValidator to validate the bound command.");

                bool surfacesMessages =
                    form.Contains("ValidationSummary", StringComparison.Ordinal)
                    || form.Contains("ValidationMessage", StringComparison.Ordinal);

                surfacesMessages.Should().BeTrue(
                    $"an EditForm in {razor.RelativePath} has user-editable inputs, so it must "
                    + "surface validation through a ValidationSummary or ValidationMessage.");
            }
        }
    }

    private static bool IsInteractive(string razorText) =>
        InteractiveRenderModes.Any(mode =>
            razorText.Contains("@rendermode " + mode, StringComparison.Ordinal)
            || razorText.Contains("@rendermode RenderMode." + mode, StringComparison.Ordinal)
            || razorText.Contains("@rendermode InteractiveServerRenderMode", StringComparison.Ordinal));

    private static bool ReferencesHttpContext(string razorText) =>
        razorText.Contains("HttpContext", StringComparison.Ordinal)
        || razorText.Contains("IHttpContextAccessor", StringComparison.Ordinal);

    private static IEnumerable<string> EditFormBlocks(string razorText)
    {
        foreach (Match open in EditFormOpenRegex.Matches(razorText))
        {
            int close = razorText.IndexOf("</EditForm>", open.Index, StringComparison.OrdinalIgnoreCase);
            yield return close < 0
                ? razorText[open.Index..]
                : razorText[open.Index..(close + "</EditForm>".Length)];
        }
    }

    private static bool HasEditableInput(string formText) =>
        SelectOrTextareaRegex.IsMatch(formText)
        || InputTagRegex.Matches(formText).Any(input => !HiddenTypeRegex.IsMatch(input.Value));

    // data-* attribute selectors a JS module pins behavior to.
    private static IEnumerable<string> ReferencedSelectors(string jsText)
    {
        foreach (Match match in DataSelectorRegex.Matches(jsText))
        {
            yield return match.Groups["sel"].Value;
        }
    }

    [GeneratedRegex("<EditForm\\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex EditFormOpenRegex { get; }

    [GeneratedRegex("<(?:select|textarea)\\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SelectOrTextareaRegex { get; }

    [GeneratedRegex("<input\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex InputTagRegex { get; }

    [GeneratedRegex("type\\s*=\\s*[\"']hidden[\"']", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex HiddenTypeRegex { get; }

    [GeneratedRegex("\\[(?<sel>data-[a-z0-9-]+)[\\]=]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex DataSelectorRegex { get; }

    private static IEnumerable<RazorFile> RazorFiles() =>
        Directory.EnumerateFiles(WebComponentsRoot(), "*.razor", SearchOption.AllDirectories)
            .Select(path => new RazorFile(Relative(path), File.ReadAllText(path)));

    private static IEnumerable<RazorJsFile> RazorJsFiles() =>
        Directory.EnumerateFiles(WebProjectRoot(), "*.razor.js", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => new RazorJsFile(Relative(path), File.ReadAllText(path)));

    private static string WebComponentsRoot() =>
        Path.Combine(WebProjectRoot(), "Components");

    private static string WebProjectRoot() =>
        Path.Combine([RepositoryRoot(), .. WebRoot]);

    private static string Relative(string fullPath) =>
        Path.GetRelativePath(RepositoryRoot(), fullPath).Replace('\\', '/');

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BrightPay.TakeHome.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (no BrightPay.TakeHome.slnx above the test binaries).");
    }

    private sealed record RazorFile(string RelativePath, string Text);

    private sealed record RazorJsFile(string RelativePath, string Text);
}
