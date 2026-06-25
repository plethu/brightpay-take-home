# BrightPay Take-Home Agent Instructions

## Task sign-off gate (every task)

Before reporting any task as done, run `just check-all` and state the result in
the handoff. It chains the fast gate (`just check`: build, unit + component
tests, format, infra validation) and the containerized `just e2e` (Playwright
browser flows + Lighthouse against the production `app` image).

`just check` alone is NOT sufficient for sign-off. E2E is deliberately kept out
of `just check` so the inner-loop gate stays fast; `just e2e` exercises the
published container and the live Blazor circuit, where defects invisible to
`just check` and to a markup-only code reading appear (e.g. a publish that drops
`_framework/blazor.web.js`, leaving the circuit dead and only the no-JS path
working). If E2E genuinely cannot run, say so explicitly rather than implying
the suite passed.

- Target .NET 10 and keep the app reviewable with `just check`.
- Read `docs/SPEC.md` before changing product behavior or scope.
- Read `.agents/skills/brightpay-takehome-workflow/SKILL.md` before planning
  take-home scope or handoff notes.
- Read `.agents/skills/brightpay-dotnet-quality/SKILL.md` before changing C#,
  project files, package versions, tests, or architecture boundaries.
- Read `.agents/skills/brightpay-blazor-frontend/SKILL.md` before changing
  Razor components, CSS, forms, accessibility, or browser tests.
- Keep domain logic in `src/BrightPay.TakeHome.Core`; keep hosting and UI in
  `src/BrightPay.TakeHome.Web`.
- Build a server-rendered Blazor Web App with targeted Interactive Server
  components. Progressive enhancement is required: no-JS paths must work by
  default and disabled-JS flows belong in E2E coverage. The
  `brightpay-blazor-frontend` skill owns the render-mode and state details.
- Keep Azure/OpenTofu deployment optional and cost-capped.
- Verify new package versions from live package metadata before pinning them.
- Use GitHub Actions for this interview repo, even though personal projects
  normally use Codeberg/Forgejo.
- Keep host developer commands native on Windows, macOS, and Linux. Do not add
  Bash-only host scripts; Linux-only scripts are acceptable inside containers.

## Definition of done for Blazor changes

After any Razor/CSS/JS change, report in the handoff against the
`brightpay-blazor-frontend` skill's "Anti-patterns — do not reintroduce"
checklist. Those idioms are enforced at build time by `BlazorConventionTests`
and the E2E suite (named checks listed in the skill), so a green
`just check-all` is necessary but not sufficient — still confirm the
micro-animations fire in a running browser, since a markup-only diff hides lost
motion when the rendered value is unchanged.
