using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.UnityOfWork;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace FiapX.Application.UseCases.VideoProcessing
{
    public class ProcessVideoUseCase : IProcessVideoUseCase
    {
        private readonly IVideoBatchRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _storageService;
        private readonly ILogger<ProcessVideoUseCase> _logger;

        public ProcessVideoUseCase(
            IVideoBatchRepository repository,
            IUnitOfWork unitOfWork,
            IFileStorageService storageService,
            ILogger<ProcessVideoUseCase> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task ExecuteAsync(ProcessVideoInput input)
        {
            const int MaxRetries = 5;
            var random = new Random();

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await ProcessInternalAsync(input);
                    return;
                }
                catch (ConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict detected for Video {input.VideoId}. Attempt {attempt}/{MaxRetries}.", input.VideoId, attempt, MaxRetries);

                    if (attempt == MaxRetries)
                    {
                        throw;
                    }

                    await _repository.ClearChangeTracker();

                    await Task.Delay(random.Next(200, 1000));
                }
            }
        }

        private async Task ProcessInternalAsync(ProcessVideoInput input)
        {
            var batch = await _repository.GetBatchWithVideosAsync(input.BatchId);
            if (batch == null) return;

            var video = batch.Videos.FirstOrDefault(v => v.Id == input.VideoId);

            if (video == null || video.Status == VideoStatus.Done) return;

            try
            {
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var demoFile = archive.CreateEntry($"frame_{DateTime.UtcNow.Ticks}.txt");
                    using var entryStream = demoFile.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync("Fake Image Content from Video " + video.FileName);
                }

                memoryStream.Position = 0;

                var zipFileName = $"{batch.Id}/{video.Id}_processed.zip";
                var zipUrl = await _storageService.UploadFileAsync(memoryStream, zipFileName, "videos-zip");

                video.MarkAsDone(zipUrl);
                batch.UpdateStatus();

                _repository.Update(batch);
            }
            catch (Exception ex)
            {
                video.MarkAsError(ex.Message);
            }

            await _unitOfWork.CommitAsync();
        }
    }
}