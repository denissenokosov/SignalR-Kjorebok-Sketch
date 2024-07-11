using System.Collections;

namespace WebApplication1.BusinessLogic;

public class Delivery
{
   public long Id { get; set; }
   public int Priority { get; set; }
   public long CreatedByUserId { get; set; }
}

public class DeliveryDay
{
    public DateOnly Date { get; set; }
    private Dictionary<long, Delivery> Deliveries { get; set; }
    private SortedList<int, Delivery> SortedDeliveries { get; set; }

    public DeliveryDay()
    {
        Deliveries = [];
        SortedDeliveries = [];
    }

    public void addDelivery(Delivery delivery)
    {
        delivery.Priority = Deliveries.Values.Count > 0 ? Deliveries.Values.Select(e => e.Priority).Max() + 1 : 0;
        Deliveries.Add(delivery.Id, delivery);
        SortedDeliveries.Add(delivery.Priority, delivery);
    }

    public void deleteDelivery(Delivery delivery)
    {
        Delivery deletedDelivery;
        Deliveries.Remove(delivery.Id, out deletedDelivery);
        if (deletedDelivery != null)
        {
            SortedDeliveries.Remove(deletedDelivery.Priority); 
        }
       
    }

    public void UpdateDeliveries(Delivery first, Delivery second)
    {
        var firstDelivery = Deliveries.GetValueOrDefault(first.Id);
        var secondDelivery = Deliveries.GetValueOrDefault(second.Id);
        if (firstDelivery == null || secondDelivery == null)
        {
            throw new Exception("Swap error");
        }
        (firstDelivery.Priority, secondDelivery.Priority) = (secondDelivery.Priority, firstDelivery.Priority);
    }

    public List<Delivery> GetDeliveries()
    {
        return Deliveries.Values.ToList();
    }
    
    
    
}


public class User
{
    public long Id { get; set; }
    public long GroupId { get; set; }
}
