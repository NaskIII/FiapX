using Azure.Messaging.ServiceBus;
using FiapX.Infrastructure.Services;
using FiapX.Infrastructure.Settings;
using Moq;
using System.Text.Json;

namespace FiapX.UnitTests.Infrastructure;

public class AzureServiceBusServiceTests
{
    private readonly Mock<ServiceBusClient> _clientMock;
    private readonly Mock<ServiceBusSender> _senderMock;
    private readonly FiapXSettings _settings;
    private readonly AzureServiceBusService _service;

    public AzureServiceBusServiceTests()
    {
        _clientMock = new Mock<ServiceBusClient>();
        _senderMock = new Mock<ServiceBusSender>();

        _settings = new FiapXSettings
        {
            ServiceBus = new ServiceBusSettings
            {
                QueueName = "fila-padrao-do-appsettings"
            }
        };

        _clientMock
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns(_senderMock.Object);

        _service = new AzureServiceBusService(_clientMock.Object, _settings);
    }

    [Fact]
    public void Constructor_Should_CreateSender_Using_QueueName_From_Settings()
    {
        _clientMock.Verify(x => x.CreateSender("fila-padrao-do-appsettings"), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_Should_SerializeMessage_And_SendToServiceBus()
    {
        var messageDto = new { Id = 123, Text = "Teste Unitário" };
        var expectedJson = JsonSerializer.Serialize(messageDto);

        var queueParam = "fila-ignorada";

        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.PublishAsync(messageDto, queueParam);

        _senderMock.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(msg => msg.Body.ToString() == expectedJson),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}