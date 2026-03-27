namespace MessageClient.Interfaces;

public interface IMessageHandler<in T>
{
    public Task Handle(T message, CancellationToken cancellationToken);
}