---
name: brightpay-blazor-frontend
description: Build or review browser UI in this BrightPay Blazor take-home repository. Use for Razor components, Blazor render modes, forms, validation messages, CSS, accessibility, responsive layouts, component tests, Playwright smoke tests, and removing generated sample UI once the real spec arrives.
---

# BrightPay Blazor Frontend

## Rendering & State

- Build the requested workflow as the first screen. Do not add a marketing
  landing page unless the spec asks for one.
- Prefer static server rendering. Add an interactive render mode only where
  stateful browser interaction genuinely needs it, and apply `@rendermode`
  per-component rather than app-wide.
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
- Keep CSS in component-scoped files, or `wwwroot/app.css` when it is global.

## Accessibility

- Use semantic HTML and native controls (labels, fieldsets, buttons, links,
  validation summaries) before reaching for custom visual controls.
- For behavior-heavy widgets such as dialogs, menus, popovers, comboboxes,
  tabs, and data grids, prefer a mature Blazor component or primitive library
  over hand-rolled ARIA. Microsoft Fluent UI Blazor is the current first
  candidate; evaluate QuickGrid, MudBlazor, Radzen, Havit, or Blazorise only
  when the spec needs their surface area. Headless Blazor primitives exist but
  are less established than Radix/React Aria in the React ecosystem.
- Associate every input with a label. Wire `ValidationMessage` to its field and
  surface form-level errors in a `ValidationSummary`.
- Mark async and validation status regions with `aria-live` so updates are
  announced.
- Preserve keyboard navigation and visible `:focus-visible` states.
- Use logical CSS properties (`margin-inline`, `padding-block`) and stable
  responsive constraints so text and controls never overlap or clip at any
  viewport. Reach for OKLCH when defining color.

## UI Quality

- Make common workflows efficient and scan-friendly.
- Use compact, work-focused styling for payroll, admin, or operational software.
  Avoid decorative card-heavy SaaS layouts.
- Ensure loading, empty, validation-error, and failure states are visible where
  the workflow can reach them.
- Remove the generated Counter and Weather pages when implementing the real
  task unless they are deliberately repurposed.

## Testing

- Use bUnit for component behavior once the package is added and restored.
- Use Playwright for at least one end-to-end browser smoke path.
- Prefer tests that assert user-visible behavior over implementation details.

## Verification

```bash
just test-components
just test-e2e-host
```

If browser dependencies are missing, record the install command and the unrun
test layer in the handoff.
