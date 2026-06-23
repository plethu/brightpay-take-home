# Dependency Notes

Pinned packages currently come from the .NET 10 templates or NuGet metadata
checked on 2026-06-23.

Candidate packages to add only when the spec needs them:

- FluentValidation for non-trivial form or input validation.
- Serilog.AspNetCore for structured request logging.
- OpenTelemetry.Extensions.Hosting for traces and metrics.
- ArchUnitNET or NetArchTest for executable architecture rules.
- SonarAnalyzer.CSharp for an extra security/code-smell pass if the warnings
  are reviewed and tuned rather than blindly suppressed.
- Microsoft.AspNetCore.Components.QuickGrid for sortable/filterable tables that
  do not justify a full component suite.
- Microsoft Fluent UI Blazor for richer accessible controls where native
  elements become brittle. Prefer it over hand-rolled ARIA for dialogs, menus,
  comboboxes, and data-heavy app surfaces.
- MudBlazor, Radzen.Blazor, Havit.Blazor, or Blazorise only when the spec needs
  a broader component system. Pick one deliberately; do not mix suites.

Do not add authentication, persistence, queues, cloud resources, or heavy UI
libraries until the official take-home spec justifies them.

Already pinned for quality gates:

- Meziantou.Analyzer and Roslynator.Analyzers for additional Roslyn coverage.

Pinned for the D1 checkout foundation:

- NodaMoney for currency-aware money values instead of custom money arithmetic.
- Microsoft.EntityFrameworkCore.SqlServer and Microsoft.EntityFrameworkCore.Design
  for the SQL Server-backed Product and Offer catalogue.
