# 25/6/26 building the ui in blazor, coming from js frameworks

_Same deal as the brainstorm files: I wouldn't normally commit a retro, but it
shows how I pick up a stack I'm new to. Writing it the evening I finished the
animation pass, while the annoyance is still fresh._

My background is React, TanStack Start, Next, React Router, Svelte/SvelteKit, Nuxt,
Vue, plus Laravel and Symfony on the server. This is the first UI of any size I've
done in Blazor. Some of it was friction, a lot carried over better than I expected,
and I want both written down before I forget which was which.

## Where the friction was

### No Framer Motion, no AnimatePresence

The big one, and where basically all of today's commits went. In React I grab
`AnimatePresence` without thinking about it: wrap the list, mount/unmount, enter and
exit transitions for free. Blazor has nothing like it in the box.

In Interactive Server mode Blazor owns the DOM, so the React "animate on unmount"
habit has nowhere to live — the moment state clears, the node's already gone and
there's nothing left to transition out. So I rebuilt the three motions by hand off
render state and CSS:

* **Pulse on change**: `@key="@value"` on the node so Blazor discards and recreates
  it when the value changes, which restarts the CSS animation. Lost a good while to
  the fact that `transform: scale()` is ignored without warning on an inline
  `<span>` (the node has to be block / flex-item / inline-block first).
* **Enter on activation**: render the node only while its state holds (`@if`), so
  Blazor creates it on the false->true edge and the enter animation plays once.
* **Exit on deactivation**: keep a ghost of the prior state for one removal window,
  collapse it with CSS, then drop it. That's the `_leavingLines` / `_leavingOffers`
  dictionaries plus a delayed re-render.

The annoying part: an earlier cleanup pass deleted the `.razor.js` that used to
drive all of this, and because a markup-only diff renders the same value, every
animation broke with zero test failures. You only catch it in a running browser,
which is exactly where nobody's looking during a "tidy up the JS" commit.

So, to stop the next person (or model) reinventing the JS hack: I wrote the three
flows up as a first-class "Interaction animations" section in
`.agents/skills/brightpay-blazor-frontend/SKILL.md`, added a Definition-of-done item
in `AGENTS.md` that names the silent-failure trap, and let `BlazorConventionTests`
fail the build if a `.razor.js` starts poking the DOM again.

### State boundaries are explicit now

In a SPA the client holds state and the server's an API call away. Blazor Server
splits one component's life across a prerender pass and a live circuit, and that
handoff is something I had to keep in mind on every interactive component. Read
`HttpContext` during prerender and you can't hang onto it — it's gone the moment the
circuit's live. The session id has to cross the boundary through
`PersistentComponentState` (`RegisterOnPersisting` + `TryTakeFromJson`), or the
basket is gone on reload, which I hit before I'd wired the persistence bridge up.

Coming from React it felt like pointless ceremony at first, then it clicked: the
ceremony's the honest version of a boundary I normally paper over with a fetch.

So the `HttpContext`-as-component-state trap now has a named convention test, and
reload survival is an E2E check (`CheckoutBasketSurvivesPageReload`) so the handoff
can't rot without something going red.

### Can't lean on "just write some JS"

Half my usual toolkit is off the table here. MutationObserver, `innerHTML`,
`requestSubmit`, IntersectionObserver, a quick `document.querySelector` to nudge
something — all of it collides with Blazor's diffing, because in interactive mode the
framework renders that DOM from the component tree and overwrites whatever you
changed underneath it. Anything I'd normally script against the rendered tree has to
become render state plus CSS. The mental flip is that the component graph is the
source of truth and the DOM hangs off it, where in plain JS the DOM is the state.

So I codified "drive enter/leave/pulse from state and CSS" as the rule, with
`RazorJsModulesDoNotMutateOrObserveTheDom` and `RazorJsSelectorsExistInMarkup` as
build-time guards, plus a rule that any `.razor.js` has to open by naming the browser
capability that needs JS.

### Three render modes meant I read the wrong docs

This one burned me more than once, honestly. Blazor has static SSR, Interactive
Server, and Interactive WebAssembly, and the old "Blazor Server" hosting model
muddies the search results on top of that. I'd land on a docs page or a Stack
Overflow answer, follow it, and only later realise it assumed WASM (client-side DI,
HttpClient to an API) or assumed pure static SSR (no circuit, no interactivity). The
symptoms are sneaky because plenty of code compiles fine in the wrong mode and just
behaves differently at runtime.

So I pinned the render-mode rules at the top of the skill — server-rendered Web App,
Interactive Server only where the workflow needs it, `@rendermode` scoped to the
smallest subtree, progressive enhancement first — so future work starts from the mode
this app uses instead of rediscovering it.

### Agents are weaker on Blazor out of the box

The models I drove were visibly less fluent here than in React. More confidently
wrong idioms, more reaching for a JS hack, more mixing of render-mode advice. Makes
sense given the training data: there's an order of magnitude more React out there
than Blazor.

So I stopped leaning on the model knowing the idiom and started encoding idioms as
build-failing checks. Prose suggestions drift across models; `BlazorConventionTests`
and the E2E suite fail the same way regardless of which model ran or whether it read
the skill. Steer and scrutinise, then verify before trusting.

## What carried over cleanly

A lot of it, more than I'd braced for going in.

### Backend domain design is just backend domain design

Architecting the offer system felt identical to designing extensible services in any
language, almost suspiciously so. The evaluator strategy plus registry
(`IOfferEvaluator`, `OfferEvaluatorRegistry`, keyed on offer type) is the same
open/closed structure I'd build in TypeScript or PHP. Five years of designing for
extension applied straight across; the language was a detail.

### My hobbyist game-dev C# carried over

I was surprised just how well my hobbyist Unity/Godot C# knowledge and experience
carried across to ASP.NET. More than the syntax, knowing the ecosystem — what's idiomatic, which
dependencies to pull in, how to keep allocations and records clean — let me write
code that reads like it belongs here rather than like a JS dev cosplaying C#.

### Docker, justfiles and CI carried over exactly

Same muscle memory from homeservers, personal projects, and onboarding new starters
at my last job. The compose setup, the `just` recipes as the single command surface,
the GitHub Actions wiring — none of it was new work, just pointed at a new repo. The
instinct to treat dev setup as a first-class concern pays off the way it always does.

### Steering agents carried over

Composing agent instructions, scrutinising output instead of vibe-coding, encoding
the rules so they survive — all the same. If anything Blazor made this skill more
valuable, since the models needed firmer steering than they do in React.

### Bits of Blazor and .NET that felt nice

* **DI as a first-class citizen.** The framework's wired through DI from the start,
  so after years of bolting various DI shims onto JS apps it felt clean.
* **Components injecting services.** `@inject` straight into a component still feels
  a bit like magic, not gonna lie. No prop-drilling a client down the tree, no
  context-provider gymnastics; the component declares what it needs and gets it.
* **One language top to bottom.** The component language being the backend language
  makes it feel like one system: no serialization seam between "the API types" and
  "the view types", no duplicated DTOs. More coherent than the React-plus-API split
  I'm used to.

### Design workflow held, with one snag

My usual flow worked fine: a few fast vibes-driven mockups I critique on feel, then
real-world study (user persona, device constraints — this is a POS so it's a tablet
or touchscreen, which made touch targets a real CSS concern), then a final
interactive mockup. That sequence is still efficient and I'd run it again.

The snag is that last interactive mockup. I build those in plain HTML/CSS/JS, and the
JS doesn't translate to Blazor the way it does to React. In React the mockup's
interaction logic is mostly portable to the real component. Here the JS I write to
make a static mockup feel alive is throwaway, since the real version has to be
render-state-driven, so the mockup proved the look but not the interaction model and
I rebuilt the motion from scratch anyway (see the whole first half of this doc).

Next time I'll do interactive examples in a Blazor playground instead, so the
mockup's interaction model is the one I ship and I stop binning that work.
