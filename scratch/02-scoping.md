# 23/6/26 (but a couple of hours later) milestones, mvp, etc.

First markdown file was a raw _'writing down my thoughts as I'm having them'_ type of doc, but here I'll be working through what each "deliverable" should be, cutting whatever is definitely out of scope, and making judgement calls that I'll note as assumptions in the README on what _is_ in scope.

## E1: Checkout

_I originally had this split out into deliverables that followed typical story point naming but it's just not worth it for something this simple, so I'll keep it loose._

### D1: Prereqs

* [ ] EF Models for core domain entities (Product, Offer)
  * [ ] Migration
    * [ ] Update justfile and docs so that migrations are ran
* [ ] DTO for Cart
* [ ] Service for interacting with cart
  * [ ] Error on invalid state
  * [ ] Shared stateful context
* [ ] View models for cart
  * [ ] Item by quantity with relevant localized names, etc.
  * [ ] Basic price calc
* [ ] Quick and rough UI to add items, perform mutations and see total
  * [ ] Error states must show
* [ ] Relevant E2E, component, and unit tests

### D2: Offer calculation

* [ ] Architecture as described in 01
  * [ ] Only implemented Offer is "QUANTITY for PRICE" as per ~/docs/SPEC.md
* [ ] Cart service should apply offers to shown prices
  * [ ] Algorithm for application should be configurable. Keep it a constant for now that's enum backed.
* [ ] E2E tests, unit tests

### D3: Offer display

* [ ] Show applied offer
* [ ] Show base total and discounted total of cart
* [ ] Group items in an offer together

### D4: Making it look nice

* [ ] Mockups
  * [ ] Commit to scratch/ to show process
  * [ ] LLM evaluation
* [ ] Styling
* [ ] Accessibility pass using axe-core
* [ ] Keyboard nav
* [ ] Fix up component tests
* [ ] Get images for products

I'm pretty sure this is it for the MVP of the kata, so I'll revisit future epics once I confirm the scope via email tomorrow.

## Addendum — 23/6/26 (after the PDF spec landed)

_Re-read the actual PDF, and D1-D4 above mostly holds, but it was written
half-assuming a shop. Now that it's clearly a checkout till, a few spec-required
bits are only loosely implied above and a couple of things I'd planned are dead
weight. Folding these in rather than rewriting the milestones._

Gaps to fold into the existing deliverables:

* [ ] Remove a single line and clear the whole basket are both explicit Part 2
  requirements. D1 only says "perform mutations", so make remove + clear
  first-class rather than an afterthought.
* [ ] No-JS path is non-negotiable per the stack rules. Scan / remove / clear /
  total all work via plain form POST + redirect, with Interactive Server only
  enhancing the running total. Belongs in D1, with a disabled-JS E2E test.
* [ ] Deliberately breaking the spec's suggested `ICheckout` (`Scan(string)` /
  `GetTotalPrice()`). Keeping the principle (a checkout you construct with its
  pricing rules, scan into, and read a total off) but fixing the ergonomics: a
  real money type instead of `int`, pass-by-id/SKU instead of loose strings,
  result types over thrown exceptions. Write the deviation and the why into the
  README so reviewers read it as a choice. Rules still get passed in at
  construction ("pass in a set of pricing rules each time we start a
  transaction"), not threaded through every call like the first cut did.
* [ ] Spell out the graceful-failure cases the spec asks about: unknown SKU,
  empty/whitespace scan, clear-on-empty. Each gets a visible, announced message
  (aria-live), not a swallowed no-op.
* [ ] The four UI states need to actually exist where the workflow can reach them:
  empty basket, item added, validation error, running total. D1's "error states
  must show" was too vague.
* [ ] Cashier needs the SKU price list visible as a reference, separate from the
  basket, with active offers shown against the relevant SKU (sharpens D1 "see
  available SKUs" and D3 "show applied offer").
* [ ] Communicate the _saving_, not just a discounted number. Show base total, the
  offer that fired, and how much it knocked off. "Why is it cheaper" is the
  cashier's actual question (tightens D3).
* [ ] Apply the EF migrations on deploy. The Tofu config provisions an empty
  Azure SQL database, so once the migrations land D1 also needs to run them,
  either a startup `MigrateAsync` in the app or a one-off init step. There's a
  matching `TODO(D1)` in `infra/opentofu/main.tf`. Without it the container boots
  against a schema-less database.

Cutting now that it's a till, not a shop:

* [ ] Product images / thumbnails (was in D4). A till doesn't need photos of
  apples, and it drags in the whole storage/CDN tangent from 01.
* [ ] Anything multi-offer-type. Spec only has "X for Y", so the registry / per
  type service chain from 01 stays a one-implementation seam, not a framework.
* [ ] Cross-tab / cross-session cart persistence and the redis question. Cart is
  per session for the demo, don't gold-plate it.

