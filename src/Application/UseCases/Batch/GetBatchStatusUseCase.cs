using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;

namespace FiapX.Application.UseCases.Batch
{
    public class GetBatchStatusUseCase : IGetBatchStatusUseCase
    {
        private readonly IVideoBatchRepository _repository;
        private readonly IFileStorageService _storageService;

        public GetBatchStatusUseCase(
            IVideoBatchRepository repository,
            IFileStorageService storageService)
        {
            _repository = repository;
            _storageService = storageService;
        }

        public async Task<BatchStatusOutput?> ExecuteAsync(Guid batchId)
        {
            var batch = await _repository.GetBatchWithVideosAsync(batchId);

            if (batch == null) return null;

            var videosDto = batch.Videos.Select(v =>
            {
                string? downloadUrl = v.OutputPath;

                if (v.Status == VideoStatus.Done && !string.IsNullOrEmpty(downloadUrl))
                {
                    downloadUrl = GenerateSignedUrl(downloadUrl);
                }
                else
                {
                    downloadUrl = null;
                }

                return new VideoStatusDto(
                    v.Id,
                    v.FileName,
                    v.Status.ToString(),
                    v.ErrorMessage,
                    downloadUrl
                );
            }).ToList();

            return new BatchStatusOutput(
                batch.Id,
                batch.Status.ToString(),
                batch.CreatedAt,
                videosDto
            );
        }

        private string GenerateSignedUrl(string rawUrl)
        {
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)) return rawUrl;

            var containerName = "videos-zip";

            var pathSegments = uri.LocalPath.Split(new[] { containerName + "/" }, StringSplitOptions.None);

            if (pathSegments.Length < 2) return rawUrl;

            var blobName = pathSegments[1];

            return _storageService.GenerateSasToken(containerName, blobName);
        }
    }
}
