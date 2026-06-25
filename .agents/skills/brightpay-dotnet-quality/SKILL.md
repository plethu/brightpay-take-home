---
name: brightpay-dotnet-quality
description: Build or review C#/.NET code in this BrightPay take-home repository. Use for backend/domain architecture, dependency choices, validation, error handling, async boundaries, tests, analyzers, package management, and maintainability reviews of .cs, .csproj, .slnx, Directory.Build.props, or Directory.Packages.props changes.
---

# BrightPay .NET Quality

## Domain Patterns

- Model domain state explicitly. Prefer enums, records, and small value objects
  over bools and strings that callers must interpret. Make illegal states
  unrepresentable instead of guarding them after the fact.
- Validate at the boundary that owns the invariant: value-object constructors or
  factory methods, request models, form models, or service entry points. A
  constructed domain type should always be valid.
- Represent money as a dedicated decimal-backed type with explicit currency and
  rounding. Do not scatter raw `decimal`/`double` arithmetic across call sites.
- Return one explicit result per operation (an outcome/result type). Avoid
  mutation-plus-`out` params and exceptions for expected outcomes.
- Make commands idempotent when retry is plausible. Include correlation,
  version, or concurrency tokens when the frontend can submit duplicate or
  stale writes.
- Shape query results for the UI explicitly. Include stable IDs and projection
  versions/ETags when the frontend needs optimistic updates or server-driven
  cache invalidation.
- Put RNG, the clock, and "now" behind seams (`TimeProvider`, injected
  interfaces) so date- and money-sensitive logic is deterministically testable.
  No `DateTime.Now` in the core.
- Keep the core free of hosting, DI-container, HTTP, and Razor types.
  Dependencies point inward; adapters bridge at the Web boundary, never to
  preserve a weak core abstraction.
- Keep transport DTOs and view models at the Web boundary, mapped from domain
  types; do not expose domain entities directly or let DTO shapes drive the
  domain model.
- Use `async` only at I/O boundaries.
- Evaluate command/query handlers, dispatch boundaries, pipelines or
  chain-of-responsibility flows, projectors, caching, and domain events only
  when the workflow complexity warrants them. Treat these as review prompts, not
  defaults.

## Dependency Bias

Add dependencies only when they remove real complexity and fit a take-home
review. Keep speculative packages in docs until verified and needed. When a
capability is justified, prefer ecosystem-aligned .NET primitives and packages
that ease migration, testability, and reviewer understanding. Use:

- FluentValidation (the core package, not the AspNetCore adapter) for
  non-trivial input validation; call validators explicitly from handlers.
- Serilog.AspNetCore for structured request logging.
- OpenTelemetry.Extensions.Hosting with the ASP.NET Core instrumentation and a
  console exporter for traces and metrics.
- bUnit for component tests.
- AwesomeAssertions for readable, fluent-style assertions.
- TngTech.ArchUnitNET when architecture boundaries need executable checks.
- Meziantou.Analyzer for additional analyzer coverage.
- Riok.Mapperly for domain-to-DTO/view-model mapping. It is a source generator,
  so the mapping code stays inspectable and allocation-free; keep mapping
  declarations next to the DTOs they produce.

Use the built-in `TimeProvider` for the clock seam; no package is needed. Verify
current package metadata before pinning new versions.

### Dependency gate

- A `PackageReference` (or `ProjectReference`) with no compile-time usage outside
  test infrastructure is a review failure. A new dependency MUST land with a
  wired call site in the same change; do not add a package "to use later".
- This is enforced at build time by ReferenceTrimmer (wired in
  `Directory.Build.props`); with `TreatWarningsAsErrors` an unused reference
  fails `just build`. Treat a green build as necessary, not sufficient — still
  confirm the call site reads as intended.
- Do not duplicate a magic number across the C#/CSS boundary. Define one token
  (a named constant or CSS custom property) and, where the same value must be
  mirrored on the other side, document the mirror with a comment pointing back to
  the source of truth.

## Review Checklist

- Are domain, UI, persistence, and infrastructure concerns separated?
- Can invalid states be represented, or are callers expected to remember rules?
- Are public APIs small and stable enough for the take-home scope?
- Are errors actionable without parsing free-form strings?
- Is date/money logic deterministic and seam-injected, not bound to wall-clock?
- Do command/query contracts support retry, stale-write detection, and focused
  frontend invalidation?
- Do tests cover the behavior that would matter to a reviewer?
- Are generated/sample files removed once the real spec is implemented?
- Does any file carry multiple concerns that should be split by responsibility?

## Verification

Run the narrowest relevant check first, then the full gate:

```bash
just test CheckoutTotals   # narrowest: filter unit + component tests to what changed
just check                 # build, tests, format, infra (no E2E)
```
