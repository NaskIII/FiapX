using System.Threading.Tasks;

namespace FiapX.Core.Interfaces.Notifications
{
    public interface IEmailTemplateService
    {
        Task<string> GetSuccessEmailTemplateAsync(string username, string batchId);
        Task<string> GetErrorEmailTemplateAsync(string username, string batchId, string errorMessage);
    }
}