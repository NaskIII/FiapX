using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface IDownloadBatchZipUseCase
    {
        Task ExecuteAsync(Guid batchId, Guid userId, Stream outputStream);
    }
}
