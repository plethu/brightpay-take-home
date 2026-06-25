using BrightPay.TakeHome.Core.Checkout.Basket;
using Microsoft.Extensions.Caching.Memory;

namespace BrightPay.TakeHome.Web.Features.Checkout.State;

public sealed class MemoryCacheCheckoutBasketStore : ICheckoutBasketStore
{
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(2);
    private readonly IMemoryCache _cache;

    public MemoryCacheCheckoutBasketStore(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public BasketSnapshot Read(string sessionId)
    {
        return string.IsNullOrEmpty(sessionId)
            ? BasketSnapshot.Empty
            : _cache.TryGetValue(sessionId, out BasketSnapshot? basket) ? basket! : BasketSnapshot.Empty;
    }

    public void Write(string sessionId, BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(basket);

        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        if (basket.IsEmpty)
        {
            _cache.Remove(sessionId);
        }
        else
        {
            _ = _cache.Set(sessionId, basket, new MemoryCacheEntryOptions
            {
                SlidingExpiration = SlidingExpiration,
            });
        }
    }

    public void Clear(string sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            _cache.Remove(sessionId);
        }
    }
}
