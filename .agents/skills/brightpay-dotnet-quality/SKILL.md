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
- Use `async` only at I/O boundaries. Do not add background work, queues, or
  hosted services until the spec requires them.

## Dependency Bias

Add dependencies only when they remove real complexity and fit a take-home
review. Keep speculative packages in docs until verified and needed. When a
capability is justified, use:

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

## Verification

Run the narrowest relevant check first, then the full gate:

```bash
just test-unit
just check
```
