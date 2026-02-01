using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface IGetBatchStatusUseCase
    {

        public Task<BatchStatusOutput?> ExecuteAsync(Guid batchId);
    }
}
