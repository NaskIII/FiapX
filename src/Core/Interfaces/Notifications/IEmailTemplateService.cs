using System.Threading.Tasks;

namespace FiapX.Core.Interfaces.Notifications
{
    public interface IEmailTemplateService
    {
        Task<string> GetSuccessEmailTemplateAsync(string username, string batchId, string generatedToken);
        Task<string> GetErrorEmailTemplateAsync(string username, string batchId, string errorMessage);
    }
}