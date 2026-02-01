using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Interfaces.Repositories;

namespace FiapX.Application.UseCases.Batch
{
    public class GetBatchStatusUseCase : IGetBatchStatusUseCase
    {
        private readonly IVideoBatchRepository _repository;

        public GetBatchStatusUseCase(IVideoBatchRepository repository)
        {
            _repository = repository;
        }

        public async Task<BatchStatusOutput?> ExecuteAsync(Guid batchId)
        {
            var batch = await _repository.GetBatchWithVideosAsync(batchId);

            if (batch == null) return null;

            var videosDto = batch.Videos.Select(v => new VideoStatusDto(
                v.Id,
                v.FileName,
                v.Status.ToString(),
                v.ErrorMessage,
                v.OutputPath
            )).ToList();

            return new BatchStatusOutput(
                batch.Id,
                batch.Status.ToString(),
                batch.CreatedAt,
                videosDto
            );
        }
    }
}
