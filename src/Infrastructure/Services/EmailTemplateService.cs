using FiapX.Core.Interfaces.Notifications;
using FiapX.Infrastructure.Settings;

namespace FiapX.Infrastructure.Services
{
    public class EmailTemplateService(FiapXSettings settings) : IEmailTemplateService
    {

        private readonly FiapXSettings _settings = settings;

        public async Task<string> GetSuccessEmailTemplateAsync(string username, string batchId, string generatedToken)
        {
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Emails", "SuccessEmail.html");
            string template = await File.ReadAllTextAsync(templatePath);

            string downloadUrl = $"{_settings.SystemUrl}/api/batches/{batchId}/download?access_token={generatedToken}";

            return template
                .Replace("{{Username}}", username)
                .Replace("{{BatchId}}", batchId)
                .Replace("{{DownloadUrl}}", downloadUrl);
        }

        public async Task<string> GetErrorEmailTemplateAsync(string username, string batchId, string errorMessage)
        {
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Emails", "ErrorEmail.html");
            string template = await File.ReadAllTextAsync(templatePath);

            return template
                .Replace("{{Username}}", username)
                .Replace("{{BatchId}}", batchId)
                .Replace("{{ErrorMessage}}", errorMessage);
        }
    }
}
