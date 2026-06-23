# BrightPay Take-Home Agent Instructions

- Target .NET 10 and keep the app reviewable with `just check`.
- Read `.agents/skills/brightpay-takehome-workflow/SKILL.md` before planning
  take-home scope or handoff notes.
- Read `.agents/skills/brightpay-dotnet-quality/SKILL.md` before changing C#,
  project files, package versions, tests, or architecture boundaries.
- Read `.agents/skills/brightpay-blazor-frontend/SKILL.md` before changing
  Razor components, CSS, forms, accessibility, or browser tests.
- Keep domain logic in `src/BrightPay.TakeHome.Core`; keep hosting and UI in
  `src/BrightPay.TakeHome.Web`.
- Prefer static server-rendered Blazor until a component needs interactivity.
- Keep Azure/OpenTofu deployment optional and cost-capped until the spec
  requires a hosted demo. Do not add authentication, persistence, queues, or
  heavy UI libraries until the official spec justifies them.
- Verify new package versions from live package metadata before pinning them.
- Use GitHub Actions for this interview repo, even though personal projects
  normally use Codeberg/Forgejo.
- Keep host developer commands native on Windows, macOS, and Linux. Do not add
  Bash-only host scripts; Linux-only scripts are acceptable inside containers.
