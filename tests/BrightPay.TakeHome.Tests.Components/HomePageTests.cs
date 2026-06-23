using BrightPay.TakeHome.Web.Components.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BrightPay.TakeHome.Tests.Components;

public sealed class HomePageTests : BunitContext
{
    public HomePageTests()
    {
        _ = Services.AddLocalization(options => options.ResourcesPath = "Resources");
    }

    [Fact]
    public void HomeRendersLocalizedHeading()
    {
        IRenderedComponent<Home> component = Render<Home>();

        component.Find("h1").TextContent.ShouldBe("BrightPay Checkout");
    }
}
