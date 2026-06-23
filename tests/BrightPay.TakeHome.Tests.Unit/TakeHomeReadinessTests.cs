using BrightPay.TakeHome.Core;

using Shouldly;

namespace BrightPay.TakeHome.Tests.Unit;

public sealed class TakeHomeReadinessTests
{
    [Fact]
    public void ItemsTrackLoadedSpecEntry()
    {
        TakeHomeReadiness.Items
            .ShouldContain(static item => item.Status == ReadinessStatus.SpecLoaded);
    }

    [Fact]
    public void ReadyCountMatchesReadyItems()
    {
        int expected = TakeHomeReadiness.Items.Count(static item => item.Status == ReadinessStatus.Ready);

        TakeHomeReadiness.ReadyCount.ShouldBe(expected);
    }
}
