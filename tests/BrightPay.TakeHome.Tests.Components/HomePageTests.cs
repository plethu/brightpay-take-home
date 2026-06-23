using BrightPay.TakeHome.Web.Components.Pages;

using Bunit;

using Shouldly;

namespace BrightPay.TakeHome.Tests.Components;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void HomePageRendersReadinessDashboard()
    {
        IRenderedComponent<Home> component = Render<Home>();

        component.Find("h1").TextContent.ShouldBe("BrightPay take-home");
        component.Markup.ShouldContain("Current State");
        component.Markup.ShouldContain("Spec loaded");
    }
}
