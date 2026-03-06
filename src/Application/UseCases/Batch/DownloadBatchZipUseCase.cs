using FiapX.Application.Interfaces;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FiapX.Application.UseCases.Batch
{
    public class DownloadBatchZipUseCase : IDownloadBatchZipUseCase
    {
        private readonly IVideoBatchRepository _repository;
        private readonly IFileStorageService _storageService;

        public DownloadBatchZipUseCase(
            IVideoBatchRepository repository,
            IFileStorageService storageService)
        {
            _repository = repository;
            _storageService = storageService;
        }

        public async Task ExecuteAsync(Guid batchId, Guid userId, Stream outputStream)
        {
            var batch = await _repository.GetBatchWithVideosAsync(batchId);

            if (batch == null || batch.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            var completedVideos = batch.Videos
                .Where(v => v.Status == VideoStatus.Done && !string.IsNullOrEmpty(v.OutputPath))
                .ToList();

            if (!completedVideos.Any())
            {
                throw new FileNotFoundException();
            }

            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true);
            var hasFiles = false;

            foreach (var video in completedVideos)
            {
                var blobName = ExtractBlobName(video.OutputPath);

                if (string.IsNullOrEmpty(blobName))
                {
                    continue;
                }

                var blobStream = await _storageService.DownloadFileAsync(blobName, "videos-zip");

                if (blobStream != null)
                {
                    var entry = archive.CreateEntry($"{video.FileName}.zip", CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await blobStream.CopyToAsync(entryStream);
                    hasFiles = true;
                }
            }

            if (!hasFiles)
            {
                throw new FileNotFoundException();
            }
        }

        private string ExtractBlobName(string rawUrl)
        {
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)) return rawUrl;

            var containerName = "videos-zip";
            var pathSegments = uri.LocalPath.Split(new[] { containerName + "/" }, StringSplitOptions.None);

            if (pathSegments.Length < 2) return rawUrl;

            return pathSegments[1];
        }
    }
}