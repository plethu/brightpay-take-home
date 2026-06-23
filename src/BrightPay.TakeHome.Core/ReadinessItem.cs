namespace BrightPay.TakeHome.Core;

public sealed record ReadinessItem(
    string Title,
    string Detail,
    ReadinessStatus Status);
