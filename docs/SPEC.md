# checkout-kata
In a normal supermarket, products are identified using Stock Keeping Units, or SKUs. In our supermarket, we’ll use individual letters of the alphabet (A, B, C, and so on). Our goods are priced individually. In addition, some items are multipriced: buy _n_ of them, and they’ll cost you _y_. For example, item ‘A’ might cost 50 pounds individually, but this week we have a special offer; buy three ‘A’s and they’ll cost you 130. The current pricing and offers are as follows:

| SKU  | Unit Price | Special Price |
| ---- | ---------- | ------------- |
| A    | 50         | 3 for 130     |
| B    | 30         | 2 for 45      |
| C    | 20         |               |
| D    | 15         |               |

Our checkout scans items individually and accepts items in any order, so that if we scan a B, an A, and another B, we’ll recognize the two Bs qualify for a special offer for a a total price of 95. You can qualify for a special offer multiple times e.g. if you scan 6 As then you will have a total price of 260. Because the pricing changes frequently, we need to be able to pass in a set of pricing rules each time we start handling a checkout transaction.

Here's a suggested interface for the checkout...
```cs
interface ICheckout
{
    void Scan(string item);
    int GetTotalPrice();
}
```

# Instructions
Implement a class or classes that satisfies the problem described above. The solution should include unit tests, and we welcome test first approaches to it.

We're as interested in the process that you go through to develop the code as the end result, so commit early and often so we can see the steps that you go through to arrive at your solution. We want to see a git repository containing your solution, ideally uploaded to your own github account. 

# Acknowledgements
Adapted from http://codekata.com/kata/kata09-back-to-the-checkout/
