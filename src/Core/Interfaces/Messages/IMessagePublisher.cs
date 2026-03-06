namespace FiapX.Core.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName);
}