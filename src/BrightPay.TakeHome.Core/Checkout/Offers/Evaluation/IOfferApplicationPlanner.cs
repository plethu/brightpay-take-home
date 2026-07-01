namespace BrightPay.TakeHome.Core.Checkout.Offers.Evaluation;

public interface IOfferApplicationPlanner
{
    OfferApplicationPlan Plan(OfferPlanningContext context);
}
