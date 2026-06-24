using AwesomeAssertions;
using BrightPay.TakeHome.Core.Checkout.Identifiers;

namespace BrightPay.TakeHome.Tests.Unit.Checkout;

public sealed class SkuTests
{
    [Theory]
    [InlineData("A")]
    [InlineData(" a ")]
    [InlineData("z")]
    public void ParseAcceptsSingleLetterSku(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Sku sku = Sku.From(input);

        sku.Value.Should().Be(input.Trim().ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("AA")]
    [InlineData("1")]
    public void TryCreateRejectsInvalidSku(string input)
    {
        Sku? sku = Sku.TryCreate(input);

        sku.Should().BeNull();
    }
}
