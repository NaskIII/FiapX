using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface IUploadVideoUseCase
    {
        public Task<UploadBatchOutput> ExecuteAsync(UploadBatchInput input);
    }
}
