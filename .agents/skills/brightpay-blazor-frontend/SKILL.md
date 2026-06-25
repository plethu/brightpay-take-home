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

## Anti-patterns — do not reintroduce

Each agent that re-derives the interaction architecture tends to invent the same
hacks. State the idiom imperatively; the named check fails the build when a
deviation ships, regardless of which model ran or whether it read this file. The
checks live in `BlazorConventionTests` (component test project) and the E2E
suite.

- Interactive components MUST NOT load state from `HttpContext` /
  `IHttpContextAccessor` as component state. Read prerender state once and flow
  it across the circuit boundary with `PersistentComponentState`
  (`RegisterOnPersisting` + `TryTakeFromJson`). A raw read with no persistence
  bridge is the hack.
  Check: `InteractiveComponentsBridgeHttpContextThroughPersistentState`.
- In interactive render modes Blazor owns the DOM. Collocated `.razor.js` modules
  MUST NOT use `MutationObserver`, `innerHTML`/`outerHTML`, `requestSubmit`, or
  broad `document.querySelector` against Blazor-rendered markup. Drive
  enter/leave/pulse from component state and CSS (`@key` remount, delayed
  removal). Check: `RazorJsModulesDoNotMutateOrObserveTheDom`.
- A `.razor.js` module MUST NOT reference selectors no markup renders. Every
  `data-*`/class selector it pins behavior to must exist in a `.razor`. Dead
  selector paths are deleted, not left dangling.
  Check: `RazorJsSelectorsExistInMarkup`.
- A control is interactive (`@onclick` + `@onclick:preventDefault`) OR a
  progressive-enhancement form POST — never both wired to a parallel JS submit.
- Every `.razor.js` module MUST open with a comment naming the browser
  capability that requires JS (one unachievable with state + CSS). An empty list
  means delete the module. Use `<ImportMap>` only for bare-specifier loading.
- An `EditForm` with user-editable inputs MUST contain a
  `DataAnnotationsValidator` and surface messages through `ValidationSummary` or
  `ValidationMessage`. Hidden-only POST wrappers (e.g. clear/charge) are exempt.
  Check: `InputBearingFormsDeclareValidation`.
- Interactive state MUST survive a full page reload (session-keyed server state +
  prerender rehydration). Check: E2E `CheckoutBasketSurvivesPageReload`.
- `@rendermode` defaults to none. Add interactivity to the smallest subtree that
  needs it; a page-level render mode needs a one-line justification.

## Interaction animations

In our interactive Server render mode Blazor owns the DOM, so micro-animations
(enter, exit, pulse) are driven by render state + CSS, never by JS that pokes the
DOM. Three flows cover everything the checkout needs; reach for these before
anything else, and verify them in a running browser (a markup-only diff hides lost
or broken motion because the rendered value is unchanged).

- Pulse on change: put `@key="@value"` on the node showing the value and the
  animation on its class. A changed key makes Blazor discard and re-create the
  node, which restarts the CSS animation; an unchanged render reuses the node and
  stays still. The animated node must be block / flex-item / grid-item /
  inline-block — `transform` is silently ignored on a non-replaced inline element,
  so a bare `<span>` will not scale. Reference: `.line-qty`, `.line-price`,
  `.checkout-amount`, `CheckoutToast`.
- Enter on activation: render the node only while its state holds (`@if`). Blazor
  creates the element exactly on the false→true transition, so an enter animation
  on its class plays once, then. Reference: offer badge `offer-in`.
- Exit on deactivation: the element is gone the instant the state clears, so there
  is nothing left to animate out. Keep a ghost of the prior state for one removal
  window, collapse it with CSS, then drop it — the page tracks the leaving item and
  re-renders after a delay matching the animation. Reference: `_leavingLines` /
  `_leavingOffers` with `line-leave` / `offer-exit`.

Coalescing (e.g. the add toast): accumulate the batch in component state across
the whole visible lifetime and re-render the running total in place; reset the
batch only when the surface actually hides, not on each update — clearing per
update restarts the count and reads as a new toast per click.

Prerendered nodes are matched and reused on hydration, so these do not spuriously
replay on initial load; a key/state change during an interactive update is what
triggers them. Every animation needs a `prefers-reduced-motion` path (handled
globally in `app.css`).

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
