using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.BusinessLogic;

public class DeliveryPlannerHub : Hub
{
    
    private readonly ActiveDeliverySessionsPool _deliverySessionsPool;
    
    
    public DeliveryPlannerHub(ActiveDeliverySessionsPool deliverySessionsPool,IHubContext<DeliveryPlannerHub> hubContext)
    {
        _deliverySessionsPool = deliverySessionsPool;
        var monitoring = new DeliveryPlannerMonitoring(_deliverySessionsPool, hubContext);
        _deliverySessionsPool.Subscribe(monitoring);
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            throw new HubException("No parameters were received");
        }

        long groupId;
        long userId;
        DateOnly date;
        
        if (!Int64.TryParse(httpContext.Request.Query["groupid"], out groupId ))
        {
            throw new HubException("Invalid group id");
        }
        if (!Int64.TryParse(httpContext.Request.Query["userid"], out userId ))
        {
            throw new HubException("Invalid user id");
        }
        if (!DateOnly.TryParse(httpContext.Request.Query["date"], out date ))
        {
            throw new HubException("Invalid date");
        }
        
        var groupIdHash = HashCode.Combine(groupId, date);
        
        _deliverySessionsPool.InitializeSessionIfNotExist(date, groupId);
        var user = new User() { GroupId = groupId, Id = userId };
        var session = _deliverySessionsPool.GetSessionForUser(date, user);
        if (session == null)
        {
            throw new HubException("Session was not found. Critical error");
        }
        
        await session.AddUser(user);

        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupIdHash.ToString());
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        
        var deliveryPlanner = new DeliveryPlannerManager(session);
        var deliveries = deliveryPlanner.GetDeliveries();
        
        await Clients.Group(userId.ToString()).SendAsync("IdentityUpdated", deliveries);
        
        _deliverySessionsPool.Notify();
        await base.OnConnectedAsync();
    }


    public async Task CreateDelivery(DateTime date, long groupId, long userId, Delivery delivery)
    {
        
        var user = new User()
        {
            Id = userId,
            GroupId = groupId
        };
        
       DateOnly dateOnly = new DateOnly(date.Year, date.Month, date.Day);
       
       var session = _deliverySessionsPool.GetSessionForUser(dateOnly, user);
       if (session == null)
       {
           throw new Exception();
       }
       
       var deliveryPlanner = new DeliveryPlannerManager(session);
       deliveryPlanner.CreateNewDelivery(delivery, user);

       var groupIdHash = HashCode.Combine(groupId, dateOnly);
       _deliverySessionsPool.Notify();
       await Clients.Group(groupIdHash.ToString()).SendAsync("NewDeliveryCreated", delivery);
    }

    public async Task RemoveDelivery(DateTime date, long groupId, long userId, Delivery delivery)
    {
        
        DateOnly dateOnly = new DateOnly(date.Year, date.Month, date.Day);
        
        _deliverySessionsPool.InitializeSessionIfNotExist(dateOnly, groupId);
        var user = new User()
        {
            Id = userId,
            GroupId = groupId
        };
        
        var session = _deliverySessionsPool.GetSessionForUser(dateOnly, user);
        if (session == null)
        {
            throw new HubException("Session was not found. Critical error");
        }
       
        var deliveryPlanner = new DeliveryPlannerManager(session);
        deliveryPlanner.RemoveDelivery(delivery);
        //_deliverySessionsPool.Notify();
        var groupIdHash = HashCode.Combine(groupId, dateOnly);
        var deliveries = deliveryPlanner.GetDeliveries();
        _deliverySessionsPool.Notify();
        await Clients.Group(groupIdHash.ToString()).SendAsync("IdentityUpdated", deliveries);
        
       // await Clients.Group(groupIdHash.ToString()).SendAsync("DeliveryRemoved", delivery);
    }

    public Task DisconnectRequested()
    {
        Context.Abort();
        return Task.CompletedTask;
    }

    public async Task UpdatePriorities(DateTime date, long groupId, long userId, Delivery deliveryFirst, Delivery deliverySecond)
    {
        var user = new User()
        {
            Id = userId,
            GroupId = groupId
        };
        
        DateOnly dateOnly = new DateOnly(date.Year, date.Month, date.Day);
       
        var session = _deliverySessionsPool.GetSessionForUser(dateOnly, user);
        if (session == null)
        {
            throw new Exception();
        }
       
        var deliveryPlanner = new DeliveryPlannerManager(session);
        deliveryPlanner.UpdatePriorities(deliveryFirst, deliverySecond, user);

        var groupIdHash = HashCode.Combine(groupId, dateOnly);
        
        var deliveries = deliveryPlanner.GetDeliveries();
        await Clients.Group(groupIdHash.ToString()).SendAsync("IdentityUpdated", deliveries);
    }
    
    
    public async Task IdentityUpdate(DateTime newDate, DateTime oldDate,  long newGroupId, long oldGroupId, long userId)
    {
        DateOnly dateOnlyNew = new DateOnly(newDate.Year, newDate.Month, newDate.Day);
        DateOnly dateOnlyOld = new DateOnly(oldDate.Year, oldDate.Month, oldDate.Day);
        
        var groupIdHashOld = HashCode.Combine(oldGroupId, dateOnlyOld);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupIdHashOld.ToString());
        
        var user = new User()
        {
            Id = userId,
            GroupId = oldGroupId
        };
        
        var session = _deliverySessionsPool.GetSessionForUser(dateOnlyOld, user);
        if (session == null)
        {
            throw new HubException("Session was not found. Critical error");
        }
        
        session.RemoveUser([user.Id]);
        
        
        var groupIdHashNew = HashCode.Combine(newGroupId, dateOnlyNew);
        _deliverySessionsPool.InitializeSessionIfNotExist(dateOnlyNew, newGroupId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupIdHashNew.ToString());

        user.GroupId = newGroupId;
        
        session = _deliverySessionsPool.GetSessionForUser(dateOnlyNew, user);
        if (session == null)
        {
            throw new HubException("Session was not found. Critical error");
        }

        await session.AddUser(user);
        
        var deliveryPlanner = new DeliveryPlannerManager(session);
        var deliveries = deliveryPlanner.GetDeliveries();
        _deliverySessionsPool.Notify();
        await Clients.Group(userId.ToString()).SendAsync("IdentityUpdated", deliveries);
    }

    
}