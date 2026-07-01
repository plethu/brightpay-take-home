namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

internal sealed record OfferSelectionScore(decimal Savings, int Priority, string CodeKey)
{
    public static OfferSelectionScore From(IReadOnlyList<OfferApplication> applications) =>
        new(
            applications.Sum(application => application.Saving.Amount),
            applications.Sum(application => application.Priority),
            string.Join('\u001f', applications.Select(application => application.Code).Order(StringComparer.Ordinal)));

    public bool IsBetterThan(OfferSelectionScore other) =>
        Savings != other.Savings
            ? Savings > other.Savings
            : Priority != other.Priority
                ? Priority < other.Priority
                : string.Compare(CodeKey, other.CodeKey, StringComparison.Ordinal) < 0;
}
