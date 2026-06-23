---
name: brightpay-takehome-workflow
description: Plan and deliver BrightPay take-home interview work in this repository. Use when choosing the next implementation slice, evaluating scope against an unknown or evolving spec, preserving visible senior AI workflow evidence, preparing handoff notes, or deciding what setup/tooling/docs belong in the take-home.
---

# BrightPay Take-Home Workflow

## Workflow

1. Re-read `docs/SPEC.md`, `AGENTS.md`, and relevant project files before
   deciding scope.
2. Keep changes PR-sized and explain why each tool, dependency, or abstraction
   helps the expected product rather than showing off.
3. Prefer boring .NET conventions first: Blazor Web App, typed domain/service
   boundaries, xUnit, central package versions, Docker/Podman-compatible
   containers, and GitHub Actions.
4. Leave evidence of considered AI usage in durable artifacts: commit messages,
   short decision records, test coverage, review notes, and readable agent
   skills. Do not add performative AI prose to the product UI.
5. Replace placeholder/sample UI with the checkout workflow immediately. Keep
   only scaffold that serves `docs/SPEC.md`.

## Scope Decisions

- `AGENTS.md` owns the standing bans and exceptions. Apply them; do not restate
  them in changes.
- If a dependency candidate cannot be verified from live package metadata, list
  it as a candidate in docs instead of pinning a guessed version.
- Keep public-facing docs concise and grounded in actual repo state.

## Verification

Before handoff, run:

```bash
just check
```

If a command cannot run because package, network, or browser dependencies are
unavailable, state the exact failing command and what remains unverified.
