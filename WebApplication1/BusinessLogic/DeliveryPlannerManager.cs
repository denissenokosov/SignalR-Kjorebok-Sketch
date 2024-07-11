namespace WebApplication1.BusinessLogic;

public class DeliveryPlannerManager
{
    private readonly DeliveryPlannerSession _plannerSession;

    public DeliveryPlannerManager(DeliveryPlannerSession deliverySessionPlanner)
    {
        _plannerSession = deliverySessionPlanner;
    }

    public bool CreateNewDelivery(Delivery newDelivery, User user)
    {
        _plannerSession.CreateNewDelivery(newDelivery, user);
        return true;
    }

    public void RemoveDelivery(Delivery deliveryToDelete)
    {
        _plannerSession.DeleteExistingDelivery(deliveryToDelete);
    }

    public void UpdatePriorities(Delivery deliveryFirst, Delivery deliverySecond, User user)
    {
        _plannerSession.UpdatePriorities( deliveryFirst, deliverySecond, user);

    }

    public IEnumerable<Delivery> GetDeliveries()
    {
        return _plannerSession.GetSessionDeliveries();
    }
    
    
}