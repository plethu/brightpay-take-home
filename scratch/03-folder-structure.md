# 23/6/26 (later still) folder structure / doing it the .NET way

_Based on best effort ~10pm duckduckgo searching and also it's the middle of a massive heatwave. Symfony/Laravel follow older style src/[Type] flat structures instead of feature folders, so wanted to make sure I wasn't defaulting to something that isn't like .NET native._

Found:
* Feature-local then organised by category so src/Feature/Checkout/Models or something?
* Share in src/Common or equiv.

Other notes I mostly knew from studying up last week:
* `DbContext` is a `Repository` and a `UnitOfWork`
* `Blazor` requires source files to live under `Components/`, but then it looks like you mostly follow the same pattern as root for organisation.

## Structure sketch

```
Core/                     # domain — grouped by bounded context, no EF/Razor/hosting in here
  Checkout/               #   Sku, Money, CheckoutCart aggregate, Offer + pricing rules, domain services
  Common/                 #   shared kernel: Result/error primitives reused across features (promote sparingly!)

Web/
  Components/             # all Razor lives under here (framework requirement)
    Layout/              #   app shell
    Pages/               #   app-level routable pages (Home, Error, NotFound)
    Checkout/            #   checkout-specific reusable components (CartLine, ProductCatalog, OfferBadge...)
  Features/
    Checkout/            #   the slice's non-UI code: app services over the DbContext, Mapperly view models, endpoint mapping
  Data/                  # EF Core infra: DbContext, entity configs, migrations, seed
  Resources/             # localization (.resx)
```

Splitting into `Core/` and `Web/` is probably overkill, but it's just to help isolate the "types" of work I'm doing. Same reason I'm being so strict on folder structure up front, guardrails help speed up learning.

