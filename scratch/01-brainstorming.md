# 23/6/26 initial brainstorm

_I usually wouldn't commit braindump files, but I figured they might be useful artifacts to show how I think through problems now that the spec has landed._

_This first one is inevitably going to kind of suck, since I'm doing it without an agent just to work through my own ideas first then I'll iterate with the agent so that it challenges those ideas once I'm done._

## Infra/services

* DB - SQL Server db probably over postgres to align with microsoft stack (and iirc that's what's being used in BrightPay? TODO: double check and align this later)
* Caching:
  * Images! Thumbnails of apples or something, need to cache and serve correct thumbnail size, but usually that's a cdn side thing. TODO: investigate primitives and terraform cfg for this.
  * Session: do I want this in redis? Could store the cart with it so that it persists between tabs, but I remember blazor circuit has its own cross-tab state story so I'll look that up before deciding. For _just_ sessions, redis feels maybe a little overkill if I don't want the cart thing. Shrugging emoji for now.
  * Items and offers: unlikely to be updated very often so could use Cache-Control on those endpoints keyed by sortType, filter, etc.
* Event bus:
  * Thinking through potential events and I'm not sure any lifecycle events feel relevant for anything but telemetry purposes? And even that seems overkill for this.
* Telemetry?
  * Way overkill for scope but might as well since it's easy. Just store scrubbed user actions like "added_item_to_cart", etc.
* Logging
  * Already got Serilog and opentel in mind. Guess since telemetry _is_ such a free win that answers the above with a thumbs up.
* Storage
  * Need product images, then ideally some sharp-like process to store res-optimised versions on upload. S3-compatible minio, etc.

## Models

```
```
Product {
  sku: char(1) NOT NULL PRIMARY KEY, # <- we'll key on this for translations instead of persisting product name etc
  baseCost: Money NULL, # <- should probably be not null but idk bad experiences. Need to look into how .NET stores money types
  state: ProductState {
    ARCHIVED,
    DRAFT,
    ACTIVE,
  } NOT NULL, # <- for admin product crud, lets you soft delete old products
  image(s?) JSON/STRING/ETC. NULLABLE, # <- will figure out s3 image storage later
}

Authentication {
  # roles, claims, expiry, type, etc. my idea is have admins with crud access to products, then hook users to sign up with exclusive tesco clubcard-style signup gate?
}

# maybe redis?
Session {
  # etc.
}

# either going in the session above or just a blazor context(? don't remember their term for it. react context I mean here. I _think_ it's still context?)
Cart {
  items: { [sku: char]: count } # <- we can calculate price after offers, etc. on the client and then just validate it on the server using the same service? don't think theres a point in keeping it stored statefully, especially if that state's going to live on the server.
}

Offer {
  id: UUID (v7 or ULID) NOT NULL PRIMARY KEY, # <- again we're going to key on this for translations
  state: OfferState {
    ARCHIVED,
    DRAFT,
    ACTIVE,
  } NOT NULL DEFAULT ARCHIVED, # <- forgot that default on product oops. Best duplicate this instead of sharing a state enum in case the domain drifts and then it's a pain separating them later.
# ... see the Offers section below, this needs some architectural thinking.
}
```
```

### Offers

Needs some architecting work. Current thinking:

* Offers are of predefined `type`s/`template`s:
  * "Buy X get Y free"
  * "X% off"
  * "X for Y" (3 for 130, 2 for 45 from the example. I'm going to be honest I have no clue how to do a pound sign on linux on this american keyboard layout.)
  * Can't think of any more for now
* We need a service for each offer `type` that can...:
  * Take configuration for X, Y, etc., parsing and validating it from a stored JSON blob.
    * ...Which unfortunately means versioning the blob because I'm never repeating the mistake of unversioned json backwards compat in a database ever again.
      * ...Which means default versioning, migration pathways, default-forward. Those are problems for the future though, for now just a json blob and v1 on everything.
  * Returns a stable ID that can be localised
  * Is backed by an `Offer` in the DB, so we need a stable identifier that preferably isn't UUID based. code-based matching or something.
  * Returns a modified price and somehow marks which items it's factoring in as "touched" (i.e. in the form validation sense) so that when we calculate best available offers we don't allow an item qualifying for two different offers
* And a service that collects all of the above and exposes the actual API for interacting with Offers:
  * Given a Cart, calculate available Offer combinations by passing it through the chain of configured and ACTIVE OfferService(s). **DOMAIN ASSUMPTION**: Sort by lowest price applicable to cart. Business decision, but just seems to be what makes the most sense here. _Algorithm details need to be configurable._
  * yield available offers for frontends.

Concerns:
* If these are CRUDable, don't allow configuring a `type` that doesn't exist in the `OffersRegistry`. Dropdown
  * **UX Concern**: Human error could cause overlapping/duplicate offers. Easy validation off the top of my head is compare `configuration` json of each given `type` that's currently `ACTIVE` and hasn't `expires_at`-ed and 400 on a match.
* Almost definitely overengineering it a little, but it's mostly boilerplate and serves as a really extensible core going forward.

## Screens and browser-facing routes

`[]` means default param, `()` means optional. not based in anything, just made it up.

* Browse (`/` -> `/browse`): An index/grid of products available
  * **Likely**:
    * Filtering by category (Offers, What's new, Category) # <- TODO: Product must belong to a ProductCategory
      * `?category=(slug)` # TODO: ProductCategory needs a `slug`
    * Sort by (Relevance, Name A-Z, Price Low-High), # <- allow inverting, very load-bearing "Relevance" here. what does that mean?
      * `?sortBy=[relevance]|price|name`
      * `?sortType=[asc]|desc`
    * Search by name (Fuzzy find, rank results), # <- relevance. figured it out
      * `?search=(URL-encoded fuzzy query)`
    * List of items showing `name`, `image` (see prior notes about thumbnails), `cartQuantity`, and an Add to Cart button
  * Maybe:
    * View options: grid and list
      * Probably not needed for this scope, will start out with just grid then see how I feel from there.
  * To test: # <- A/B testing? Maybe just mock them both up and go by vibes? Usually would seek P/O or stakeholder input on mockups of both
    * Pagination (`<- Prev` `2 of 4` `Next ->`), # <- probably not
    * Infinite scroll, # <- will need to look up how blazor does this instead of using an intersection observer.
* Auth (`/auth`)
  * Redir to match:
    * Authed? `/auth/me`
    * Not authed? `/auth/begin`
* Log out (`/auth/end`)
* Log in or sign up (`/auth/begin`)
  * Likely:
    * Defaults to the signup form. Link underneath "Already got an account? Sign in instead". Should change `?intent=[signup]|login`
    * Redirects based on where they came from, forward route along as query param here? To avoid using a cookie
      * `?redirectTo=(URL-encoded route)`
        * Default here should probably just be `/`? Or `/admin` if `Authentication` is `admin`?
  * Maybe:
    * Progressive form validation if I can figure it out using blazor
  * Probably not:
    * OAuth, OIDC, Passkey (unless they're completely free with .NET, it's just too much work for a demo)
* Cart (`/cart`)
  * Likely:
    * A list with a checkout button
    * Show applied `Offer`s and calculated price from the `OffersService` or whatever it ends up being called
    * Can adjust quantities and remove items, which will re-calculate `Offer`s
    * Checkout button must be disabled if no `Product`s in `Cart`
  * Maybe:
    * "Continue shopping" button to redirect to `/browse`
* Checkout (`/cart/checkout`)
  * Likely:
    * Placeholder/mock-up only
  * DEFINITELY not:
    * Payment integration via dockerized stripe-mock or something
    * I don't have the resillience or claude code budget to work with payment providers while unemployed
* (MAYBE) Admin dashboard (`/admin`)
  * Just a crud for products and offers. I'll loop back to it once I'm done with the actual spec work.

### API

* Standardised errors: `problem+json`
* Fail-close and don't reveal on unauthorized access to a resource, 404 instead.
* Middleware:
  * Basic route logging
  * AuthN
  * Scoped AuthZ by route group
* JSON? If we can inject services like repo into our blazor components, do we even need an API for trivial stuff like resources?
  * If so, standardised DTO mappings with versioned API entities.
  * OpenAPI autogen.


