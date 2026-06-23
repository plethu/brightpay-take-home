# checkout-kata

## Part 1: The Checkout

In a normal supermarket, products are identified using Stock Keeping Units, or SKUs. In our supermarket, we'll use individual letters of the alphabet (A, B, C, and so on). Our goods are priced individually. In addition, some items are multipriced: buy n of them, and they'll cost you y. For example, item 'A' might cost 50 pounds individually, but this week we have a special offer; buy three 'A's and they'll cost you 130.

The current pricing and offers are as follows:

| SKU | Unit Price | Special Price |
| --- | ---------- | ------------- |
| A   | 50         | 3 for 130     |
| B   | 30         | 2 for 45      |
| C   | 20         |               |
| D   | 15         |               |

Our checkout scans items individually and accepts items in any order, so that if we scan a B, an A, and another B, we'll recognise the two Bs qualify for a special offer for a total price of 95. You can qualify for a special offer multiple times e.g. if you scan 6 As then you will have a total price of 260. Because the pricing changes frequently, we need to be able to pass in a set of pricing rules each time we start handling a checkout transaction.

Here's a suggested interface for the checkout:

```csharp
interface ICheckout
{
    void Scan(string item);
    int GetTotalPrice();
}
```

## Instructions

Implement a class or classes that satisfies the problem described above. The solution should include unit tests, and we welcome test first approaches to it.

## Part 2: The Checkout UI

Build a Blazor front end on top of your checkout implementation. BrightPay is a Blazor product, so we'd like to see your work in that stack. MudBlazor is welcome but not required.

Pick the component library, or none, that best supports the design decisions you want to make.

The UI should let a cashier:

- See the available SKUs and their pricing, including any active special offers
- Add items to the current transaction
- See the running total update as items are added
- Remove items or clear the basket

Beyond those requirements, the design is yours. Treat this as a thin product surface, not just a wrapper around the API.

## What we're looking for

- Component composition and reusability. We expect to see some thought about how the UI is broken down.
- A clear point of view on UX. What is the cashier's job, what information do they need, how are special offers communicated, how does the system fail gracefully.
- Accessibility considerations appropriate to the surface area.
- Visual coherence. It does not need to be production-polished, but it should feel deliberate.

## What we are not looking for

- A particular visual style, theme, or branding
- Production-grade polish
- Comprehensive UI-level tests. Unit tests on the backend remain valuable.

## Process

We're as interested in the process you go through as the end result, so commit early and often so we can see the steps you took. We want to see a git repository containing your solution, ideally on your own GitHub account.

In the second stage interview, you'll share your screen, walk us through the solution, and we'll extend it together. Come ready to talk through both the technical choices and the design decisions you made.

AI tooling is fine to use. We'd rather see something you understand and can extend than something you don't.
