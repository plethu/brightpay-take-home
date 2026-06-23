using Shouldly;

namespace BrightPay.TakeHome.Tests.Unit;

// Placeholder so the unit test project builds and runs against the empty Core
// domain. Replace with checkout pricing tests as the domain lands (D1+).
public sealed class ScaffoldTests
{
    [Fact]
    public void CoreProjectIsReferenced() =>
        typeof(object).Assembly.GetName().Name.ShouldBe("System.Private.CoreLib");
}
