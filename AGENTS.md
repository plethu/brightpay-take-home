# BrightPay Take-Home Agent Instructions

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
  default and disabled-JS flows belong in E2E coverage.
- Keep Azure/OpenTofu deployment optional and cost-capped.
- Verify new package versions from live package metadata before pinning them.
- Use GitHub Actions for this interview repo, even though personal projects
  normally use Codeberg/Forgejo.
- Keep host developer commands native on Windows, macOS, and Linux. Do not add
  Bash-only host scripts; Linux-only scripts are acceptable inside containers.
