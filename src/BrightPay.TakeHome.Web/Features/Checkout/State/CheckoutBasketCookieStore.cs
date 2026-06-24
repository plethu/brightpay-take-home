using System.Text.Json;
using BrightPay.TakeHome.Core.Checkout.Basket;
using BrightPay.TakeHome.Core.Checkout.Identifiers;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace BrightPay.TakeHome.Web.Features.Checkout.State;

public sealed class CheckoutBasketCookieStore
{
    public const string CookieName = "brightpay.checkout";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDataProtector _protector;

    public CheckoutBasketCookieStore(IDataProtectionProvider protectionProvider)
    {
        ArgumentNullException.ThrowIfNull(protectionProvider);

        _protector = protectionProvider.CreateProtector("BrightPay.TakeHome.CheckoutBasket.v1");
    }

    public BasketSnapshot Read(HttpContext? httpContext)
    {
        if (httpContext?.Request.Cookies.TryGetValue(CookieName, out string? protectedValue) != true
            || string.IsNullOrWhiteSpace(protectedValue))
        {
            return BasketSnapshot.Empty;
        }

        try
        {
            string json = _protector.Unprotect(protectedValue);
            CheckoutBasketCookie? cookie = JsonSerializer.Deserialize<CheckoutBasketCookie>(json, JsonOptions);
            if (cookie?.Lines is not { Count: > 0 })
            {
                return BasketSnapshot.Empty;
            }

            List<BasketLine> lines = [];
            foreach (CheckoutBasketCookieLine line in cookie.Lines)
            {
                Sku? sku = Sku.TryCreate(line.Sku);
                if (sku is { } validSku && line.Quantity > 0)
                {
                    lines.Add(new BasketLine(validSku, line.Quantity));
                }
            }

            return new BasketSnapshot(lines);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        {
            return BasketSnapshot.Empty;
        }
    }

    public void Write(HttpContext httpContext, BasketSnapshot basket)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(basket);

        if (basket.IsEmpty)
        {
            httpContext.Response.Cookies.Delete(CookieName);
            return;
        }

        CheckoutBasketCookie cookie = new(
            [.. basket.Lines.Select(line => new CheckoutBasketCookieLine(line.Sku.Value, line.Quantity))]);
        string json = JsonSerializer.Serialize(cookie, JsonOptions);
        string protectedValue = _protector.Protect(json);
        httpContext.Response.Cookies.Append(CookieName, protectedValue, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
        });
    }

    private sealed record CheckoutBasketCookie(IReadOnlyList<CheckoutBasketCookieLine> Lines);

    private sealed record CheckoutBasketCookieLine(string Sku, int Quantity);
}
