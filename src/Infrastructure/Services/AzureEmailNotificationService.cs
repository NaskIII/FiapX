using Azure;
using Azure.Communication.Email;
using FiapX.Core.Interfaces.Notifications;
using FiapX.Infrastructure.Settings;
using System.Threading.Tasks;

namespace FiapX.Infrastructure.Services.Notifications
{
    public class AzureEmailNotificationService : IEmailNotificationService
    {
        private readonly EmailClient _emailClient;
        private readonly string _senderAddress;

        public AzureEmailNotificationService(EmailClient emailClient, FiapXSettings fiapXSettings)
        {
            _emailClient = emailClient;
            _senderAddress = fiapXSettings.AzureCommunicationServices.SenderAddress;
        }

        public async Task SendEmailAsync(string recipient, string subject, string htmlBody)
        {
            var emailContent = new EmailContent(subject)
            {
                Html = htmlBody
            };

            var emailMessage = new EmailMessage(
                _senderAddress,
                recipient,
                emailContent
            );

            await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
        }
    }
}