namespace FiapX.Application.Interfaces
{
    public interface INotifyBatchProcessingResultUseCase
    {
        Task ExecuteSuccessAsync(Guid userId, Guid batchId);
        Task ExecuteErrorAsync(Guid userId, Guid batchId, string errorMessage);
    }
}
