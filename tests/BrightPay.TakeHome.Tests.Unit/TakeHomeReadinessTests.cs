using BrightPay.TakeHome.Core;

using Shouldly;

namespace BrightPay.TakeHome.Tests.Unit;

public sealed class TakeHomeReadinessTests
{
    [Fact]
    public void ItemsTrackAtLeastOnePendingSpecEntry()
    {
        TakeHomeReadiness.Items
            .ShouldContain(static item => item.Status == ReadinessStatus.PendingSpec);
    }

    [Fact]
    public void ReadyCountMatchesReadyItems()
    {
        int expected = TakeHomeReadiness.Items.Count(static item => item.Status == ReadinessStatus.Ready);

        TakeHomeReadiness.ReadyCount.ShouldBe(expected);
    }
}
