using BrightPay.TakeHome.Web.Components.Pages;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BrightPay.TakeHome.Tests.Components;

public sealed class CartPageTests : BunitContext
{
    public CartPageTests()
    {
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
    }

    [Fact]
    public void CartRendersLocalizedHeading()
    {
        IRenderedComponent<Cart> component = Render<Cart>();

        component.Find("h1").TextContent.Should().Be("BrightPay Checkout");
    }
}
