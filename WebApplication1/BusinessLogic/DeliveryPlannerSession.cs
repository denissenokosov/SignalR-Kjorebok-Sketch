using System.Collections.Concurrent;

namespace WebApplication1.BusinessLogic;

public class DeliveryPlannerSession
{
    
    private ConcurrentDictionary<long, User> _users;
    private readonly ConcurrentDictionary<long, DateTime> _lastAction;
    private readonly DeliveryDay _deliveryDay;
    private int _logOutTime;

    public DeliveryPlannerSession(int logOutTime)
    {
        _users = [];
        _deliveryDay = new DeliveryDay();
        _lastAction = [];
        _logOutTime = logOutTime;
    }
    
    public Task AddUser(User newUser)
    {
        var now = DateTime.UtcNow;
        var logOutTime = now.AddMinutes(_logOutTime);
        _users.TryAdd(newUser.Id, newUser);
        if (_lastAction.ContainsKey(newUser.Id))
        {
            var oldValue = _lastAction.GetValueOrDefault(newUser.Id);
            if (! _lastAction.TryUpdate(newUser.Id, logOutTime, oldValue))
            {
                return Task.FromException<Exception>(new Exception("Could not update user's last action"));
            }
        }
        else
        {
            _lastAction.TryAdd(newUser.Id, logOutTime);
        }
        return Task.CompletedTask;
    }

    public int GetNumberOfUsers()
    {
        return _users.Count;
    }
    public void RemoveUser(List<long> userIdsToDelete)
    {
        foreach (var userIdToDelete in userIdsToDelete)
        {
            User user;
            _users.Remove(userIdToDelete, out user);
        }
        
    }

    public void CreateNewDelivery(Delivery newDelivery, User newUser)
    {
        var now = DateTime.UtcNow;
        var logOutTime = now.AddMinutes(_logOutTime);
        _deliveryDay.addDelivery(newDelivery);
        if ( _lastAction.ContainsKey(newUser.Id))
        {
            var oldValue = _lastAction.GetValueOrDefault(newUser.Id);
            if (!_lastAction.TryUpdate(newUser.Id, logOutTime, oldValue))
            {
                throw new Exception("Could not update the last action");
            }
        }
        else
        {
            _lastAction.TryAdd(newUser.Id, now);
        }
    }
    
    public void UpdatePriorities(Delivery deliveryFirst, Delivery deliverySecond, User user)
    {
        var now = DateTime.UtcNow;
        var logOutTime = now.AddMinutes(_logOutTime);
        
        if ( _lastAction.ContainsKey(user.Id))
        {
            var oldValue = _lastAction.GetValueOrDefault(user.Id);
            if (!_lastAction.TryUpdate(user.Id, logOutTime, oldValue))
            {
                throw new Exception("Could not update the last action");
            }
        }
        else
        {
            _lastAction.TryAdd(user.Id, now);
        }
        
        _deliveryDay.UpdateDeliveries(deliveryFirst, deliverySecond);
    }
    

    public void DeleteExistingDelivery(Delivery deliveryToDelete)
    {
        _deliveryDay.deleteDelivery(deliveryToDelete);
    }

    public IEnumerable<Delivery> GetSessionDeliveries()
    {
        return _deliveryDay.GetDeliveries();
    }

    public ConcurrentDictionary<long, DateTime> GetUsersLastActionTime()
    {
        return _lastAction;
    }
    
}