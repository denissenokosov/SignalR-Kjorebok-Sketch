using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using NotImplementedException = System.NotImplementedException;

namespace WebApplication1.BusinessLogic;

public class ActiveDeliverySessionsPool : IObserverPublisher
{
    private readonly int _maxNumberOfSessions = 1000;
    private readonly int _logOutTimeForInactivity = 5; // in minutes
    private readonly ILogger<ActiveDeliverySessionsPool> _logger;
    private readonly ConcurrentDictionary<int ,DeliveryPlannerSession> _sessions;
    private Task _backgroundLogOutTask;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IHubContext<DeliveryPlannerHub> _hubContext;
    private readonly List<IObserverSubscriber> _subscribers;
    

    public ActiveDeliverySessionsPool(ILogger<ActiveDeliverySessionsPool> logger,
        IHubContext<DeliveryPlannerHub> hubContext)
    {
        _logger = logger;
        _sessions = [];
        _cancellationTokenSource = new CancellationTokenSource();
        _hubContext = hubContext;
        _subscribers = [];
        StartAsync(_cancellationTokenSource.Token);
    }

    private Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ActiveDeliverySessionsPool started.");
        _backgroundLogOutTask = Task.Run(() => BackgroundTask(_cancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task BackgroundTask(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                // Process users
                var now = DateTime.UtcNow;
                var userIdsToLogOut = new List<long>();
                foreach (var sessionKeyValuePair in _sessions)
                {
                    var session = sessionKeyValuePair.Value;
                    var toDeletePairs = session.GetUsersLastActionTime().Where(user =>
                        user.Value < now).Select(user => user.Key).ToList();
                    session.RemoveUser(toDeletePairs);
                    userIdsToLogOut.AddRange(toDeletePairs);
                }

                foreach (var userId in userIdsToLogOut)
                {
                    
                    await _hubContext.Clients.Group(userId.ToString()).SendAsync("Disconnect", token);
                }

                if (userIdsToLogOut.Count > 0)
                {
                    Notify(); 
                }
                
                _logger.LogInformation("Background task processed objects.");
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Background task cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background task.");
            }
        }
    }
    
    public void InitializeSessionIfNotExist(DateOnly dateOnly, long groupId)
    {
        if (_sessions.Count >= _maxNumberOfSessions)
        {
            throw new HubException("No available slots for connection"); // create custom exception 
        }
        

        var sessionHash =  HashCode.Combine(dateOnly, groupId);
        if (_sessions.ContainsKey(sessionHash))
        {
            return;
        }
       
        var newHash =  HashCode.Combine(dateOnly, groupId);
        var session = new DeliveryPlannerSession(_logOutTimeForInactivity);
        if (!_sessions.TryAdd(newHash, session))
        {
            throw new Exception("Session initialization error.");
        }
        
    }

    public DeliveryPlannerSession? GetSessionForUser(DateOnly dateOnly, User user)
    {
        var sessionHash =  HashCode.Combine(dateOnly, user.GroupId);
        DeliveryPlannerSession? session;
        _sessions.TryGetValue(sessionHash, out session);
        return session;
    }

    public List<DeliveryPlannerSession> getActiveSessions()
    {
        return _sessions.Values.ToList();
    }

    public void Subscribe(IObserverSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
    }

    public void Unsubscribe(IObserverSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
    }

    public void Notify()
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.Update();
        }
    }
}


