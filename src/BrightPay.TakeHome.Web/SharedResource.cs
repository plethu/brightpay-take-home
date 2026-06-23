namespace BrightPay.TakeHome.Web;

/// <summary>
/// Marker type for app-wide localized strings. Inject
/// <see cref="Microsoft.Extensions.Localization.IStringLocalizer{SharedResource}" />
/// and back it with <c>Resources/SharedResource.resx</c> (default culture) plus
/// culture-specific <c>Resources/SharedResource.&lt;culture&gt;.resx</c> files.
/// Kept in the root namespace so the configured ResourcesPath resolves to
/// <c>Resources/SharedResource.resx</c> rather than a nested folder.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "MA0036:Make class static",
    Justification = "IStringLocalizer<T> requires a non-static type argument.")]
public sealed class SharedResource
{
}
