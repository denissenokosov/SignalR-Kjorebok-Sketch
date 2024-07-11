using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.BusinessLogic;


public class DeliveryPlannerMonitoring : IObserverSubscriber
{
    private readonly ActiveDeliverySessionsPool _deliverySessionsPool;
    private readonly IHubContext<DeliveryPlannerHub> _hubContext;
    
    public DeliveryPlannerMonitoring(ActiveDeliverySessionsPool deliveriesPool,  IHubContext<DeliveryPlannerHub> hubContext)
    {
        _deliverySessionsPool = deliveriesPool;
        _hubContext = hubContext;
    }


    public async void Update()
    {
        DeliveryPlannerOverview overview = new DeliveryPlannerOverview();
        var sessions = _deliverySessionsPool.getActiveSessions();
        overview.numberOfSessions = sessions.Count;
        overview.numberOfDeliveries = sessions.Sum(e => e.GetSessionDeliveries().Count());
        overview.numberOfConnectedUsers = sessions.Sum(e => e.GetNumberOfUsers());
        
        await _hubContext.Clients.All.SendAsync("MonitoringUpdate", overview);
    }
}


public struct DeliveryPlannerOverview
{
    public int numberOfDeliveries { get; set; }
    public int numberOfSessions { get; set; }
    public int numberOfConnectedUsers { get; set; }

}