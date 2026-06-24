---
name: brightpay-blazor-frontend
description: Build or review browser UI in this BrightPay Blazor take-home repository. Use for Razor components, Blazor render modes, forms, validation messages, CSS, accessibility, responsive layouts, component tests, Playwright smoke tests, and replacing generated sample UI with the checkout workflow.
---

# BrightPay Blazor Frontend

## Rendering & State

- Build the checkout workflow from `docs/SPEC.md` as the first screen. Do not
  add a marketing landing page.
- Use a server-rendered Blazor Web App with targeted Interactive Server
  components for checkout scanning feedback. Do not switch to WebAssembly or
  client-only rendering.
- Apply `@rendermode` per component and only where client interactivity changes
  the user workflow.
- Progressive enhancement is required. Default routes, forms, validation, and
  checkout totals must render and complete without JavaScript; Interactive
  Server augments the no-JS path.
- Lazy-load only large feature areas, expensive JS interop, or optional
  component suites. Do not add lazy loading to hide unnecessary bundle weight.
- Hold navigable view state (filters, paging, sort, selected record) in the
  query string via `[SupplyParameterFromQuery]` and `NavigationManager`. This
  keeps state bookmarkable, shareable, and reload-safe without interactivity.
- Keep other component state local unless multiple components genuinely share it.
- When a workflow mutates server state, treat the server response as
  authoritative. Use optimistic UI only for reversible actions, keep a rollback
  path, and refresh affected projections by entity ID, version, ETag, or
  explicit invalidation event rather than by broad page reloads.
- Server-render `lang`, direction, theme, and culture data attributes on
  `<html>` from the same resolved state used for initial rendering. Key CSS and
  startup behavior from those attributes to avoid localization or theme flicker.
- Make all user-facing text localizable. Do not hard-code copy inside reusable
  components when the text belongs to the product workflow.

## Accessibility

- Aim UI work at WCAG 2.2. Automated checks are required, but manual review is
  still needed for keyboard flow, focus order, naming, motion sensitivity, and
  whether the workflow makes sense to assistive technology users.
- Use semantic HTML and native controls (labels, fieldsets, buttons, links,
  validation summaries) before reaching for custom visual controls.
- The checkout workflow does not need behavior-heavy component suites. If a
  dialog, menu, popover, combobox, tab set, or data grid becomes necessary,
  prefer a mature Blazor component or primitive library over hand-rolled ARIA;
  Microsoft Fluent UI Blazor is the first option to evaluate.
- Associate every input with a label. Wire `ValidationMessage` to its field and
  surface form-level errors in a `ValidationSummary`.
- Mark async and validation status regions with `aria-live` so updates are
  announced.
- Preserve keyboard navigation and visible `:focus-visible` states.
- Use logical CSS properties (`margin-inline`, `padding-block`) and stable
  responsive constraints so text and controls never overlap or clip at any
  viewport. Reach for OKLCH when defining color.
- Respect `prefers-reduced-motion` for transitions, animations, skeletons, and
  scroll behavior. The reduced-motion path should preserve state changes without
  relying on movement.
- When Playwright E2E paths exist, add axe-core coverage to the same path and
  fail CI on new serious or critical violations unless a temporary suppression
  is documented with scope, owner, and removal condition.
- Add accessibility linting only when the Blazor/.NET ecosystem offers a
  maintained option that fits the repo. Do not invent brittle custom linters.

## UI Quality

- Make common workflows efficient and scan-friendly.
- Use compact, work-focused styling for payroll, admin, or operational software.
  Avoid decorative card-heavy SaaS layouts.
- Keep a consistent visual language for primary, secondary, destructive,
  navigation, and inline/utility actions. Action style should communicate risk
  and hierarchy without relying on color alone.
- Small tactile interaction animations are acceptable for hover, press, focus,
  expansion, and save feedback when they are fast, subtle, and have a
  reduced-motion fallback.
- Ensure loading, empty, validation-error, and failure states are visible where
  the workflow can reach them.
## Browser Code & CSS

- Do not add a TypeScript toolchain before there is browser code. When browser
  code is justified for JS interop modules, client-only enhancement, or complex
  DOM/browser APIs, author it as TypeScript and compile to imported or
  collocated JavaScript modules.
- Prefer Blazor CSS isolation (`*.razor.css`) for component styles. Use
  `wwwroot/app.css` for tokens, cascade layers, resets, and global primitives.
- Define design tokens for color, spacing, radius, typography, motion, shadows,
  and z-index. Arbitrary one-off constants in components are review concerns
  unless the local exception is explained.
- Prefer modern CSS progressively: cascade layers, container queries with
  explicit containment, logical properties, `color-scheme`, and native
  `<dialog>` semantics or mature Blazor primitives.
- Treat `text-wrap: balance` and `text-wrap: pretty` as progressive
  enhancement. Normal wrapping must still produce readable, unclipped text.

## Testing

- Use bUnit for component behavior once the package is added and restored.
- Use Playwright for at least one end-to-end browser smoke path.
- Add disabled-JavaScript Playwright E2E coverage for the default checkout path.
- Prefer axe-core through the existing .NET Playwright E2E flow for
  accessibility checks. Verify the least-heavy maintained integration path
  before adding a separate Node test harness.
- Prefer Lighthouse CI (`@lhci/cli`) for CI vitals and performance budgets on
  known routes. Use realistic first thresholds that fail clear regressions, and
  do not upload reports to public temporary storage unless explicitly chosen.
- Defer Unlighthouse until the app has enough routes that crawling is valuable.
- Prefer tests that assert user-visible behavior over implementation details.

## Verification

```bash
just test-components
just test-e2e-host
```

If browser dependencies are missing, record the install command and the unrun
test layer in the handoff.

### Screenshots for visual critique

To see a page rendered (UI critique and iteration), drive the Chromium that
ships in the compose `e2e` image. No host browser or Node is needed; call the
bundled binary directly. Run in the **foreground** — backgrounded `docker run`
screenshots have hung here. View the PNG with the Read tool afterward.

```bash
# Static file (e.g. scratch/mockups). The chromium-* dir drifts with the image
# tag, so resolve it at call time. dbus errors printed headless are harmless.
docker run --rm -v "$PWD":/src:ro -v /tmp/shots:/out \
  mcr.microsoft.com/playwright/dotnet:v1.60.0-noble bash -lc \
  '"$(ls /ms-playwright/chromium-*/chrome-linux64/chrome)" --headless --no-sandbox \
   --disable-gpu --hide-scrollbars --force-color-profile=srgb \
   --window-size=1280,1100 --virtual-time-budget=3000 \
   --screenshot=/out/page.png "file:///src/scratch/mockups/a-two-column-till.html"'
```

For the running app, bring it up with `just up` and screenshot its URL instead
of a `file://` path (join the compose network, or use the host-published port).
The checkout mockups accept `?state=empty|added|error` and `?theme=light|dark`
so a specific state/theme can be captured without editing markup.
