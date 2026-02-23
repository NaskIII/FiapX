using Azure;
using Azure.Communication.Email;
using FiapX.Infrastructure.Services.Notifications;
using FiapX.Infrastructure.Settings;
using Moq;

namespace FiapX.UnitTests.Infrastructure
{
    public class AzureEmailNotificationServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_Should_Call_EmailClient_SendAsync_With_Correct_Parameters()
        {
            var emailClientMock = new Mock<EmailClient>();

            var settings = new FiapXSettings();
            settings.AzureCommunicationServices = new AzureCommunicationServicesSettings
            {
                SenderAddress = "sender@domain.com"
            };

            var service = new AzureEmailNotificationService(emailClientMock.Object, settings);

            var recipient = "recipient@domain.com";
            var subject = "Test Subject";
            var htmlBody = "<html><body>Test</body></html>";

            await service.SendEmailAsync(recipient, subject, htmlBody);

            emailClientMock.Verify(c => c.SendAsync(
                WaitUntil.Started,
                It.Is<EmailMessage>(m =>
                    m.SenderAddress == "sender@domain.com" &&
                    m.Content.Subject == subject &&
                    m.Content.Html == htmlBody),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
