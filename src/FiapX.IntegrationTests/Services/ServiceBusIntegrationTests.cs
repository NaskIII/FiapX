using Azure.Messaging.ServiceBus;
using FiapX.Infrastructure.Services;
using FiapX.IntegrationTests.Setup;
using FluentAssertions;

namespace FiapX.IntegrationTests.Services;

public class ServiceBusIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly AzureServiceBusService _publisherService;
    private readonly ServiceBusClient _client;
    private readonly string _queueName;

    public ServiceBusIntegrationTests(DatabaseFixture fixture)
    {
        var settings = fixture.Settings;
        _queueName = settings.ServiceBus.QueueName;

        _client = new ServiceBusClient(settings.ServiceBus.Connection);

        _publisherService = new AzureServiceBusService(_client, settings);
    }

    [Fact]
    public async Task PublishAsync_Should_Send_Message_That_Can_Be_Received()
    {
        var uniqueId = Guid.NewGuid();
        var messagePayload = new { Id = uniqueId, Action = "IntegrationTest" };

        await _publisherService.PublishAsync(messagePayload, _queueName);

        var receiver = _client.CreateReceiver(_queueName);

        var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(5));

        ServiceBusReceivedMessage? targetMessage = null;

        foreach (var msg in receivedMessages)
        {
            var body = msg.Body.ToString();
            if (body.Contains(uniqueId.ToString()))
            {
                targetMessage = msg;
                await receiver.CompleteMessageAsync(msg);
                break;
            }
        }

        targetMessage.Should().NotBeNull("A mensagem deveria ter sido encontrada na fila.");

        var jsonBody = targetMessage!.Body.ToString();
        jsonBody.Should().Contain("IntegrationTest");
    }
}