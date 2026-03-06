using Azure.Messaging.ServiceBus;
using FiapX.Core.Interfaces;
using FiapX.Infrastructure.Settings;
using System.Text.Json;

namespace FiapX.Infrastructure.Services;

public class AzureServiceBusService : IMessagePublisher
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusService(ServiceBusClient client, FiapXSettings settings)
    {
        _sender = client.CreateSender(settings.ServiceBus.QueueName);
    }

    public async Task PublishAsync<T>(T message, string queueName)
    {

        var jsonMessage = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonMessage);

        await _sender.SendMessageAsync(serviceBusMessage);
    }
}