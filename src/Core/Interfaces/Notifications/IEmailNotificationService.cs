namespace FiapX.Core.Interfaces.Notifications
{
    public interface IEmailNotificationService
    {
        Task SendEmailAsync(string recipient, string subject, string htmlBody);
    }
}