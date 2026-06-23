# 23/6/26 (but a couple of hours later) milestones, mvp, etc.

First markdown file was a raw _'writing down my thoughts as I'm having them'_ type of doc, but here I'll be working through what each "deliverable" should be, cutting whatever is definitely out of scope, and making judgement calls that I'll note as assumptions in the README on what _is_ in scope.

So assumptions up front:
* Given the interview context of wanting to see production-quality front-end and back-end work out of me, I'm assuming the scope of this project extends beyond a CLI based kata, and instead the aim is to model the problem as if it were in a production system on a webapp using Blazor and .NET.
* Given the rest of the stack, the azure focus, etc. I'm assuming SQL server is the correct SQL driver here.

TODO: Document this in the README later.

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

