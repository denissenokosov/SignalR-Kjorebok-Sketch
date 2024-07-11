namespace WebApplication1.BusinessLogic;

public interface IObserverPublisher
{
    public void Subscribe(IObserverSubscriber subscriber);
    public void Unsubscribe(IObserverSubscriber subscriber);
    public void Notify();
}