using BrightPay.TakeHome.Core.Checkout.Basket;

namespace BrightPay.TakeHome.Web.Features.Checkout.State;

public interface ICheckoutBasketStore
{
    BasketSnapshot Read(string sessionId);
    void Write(string sessionId, BasketSnapshot basket);
    void Clear(string sessionId);
}
