using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Entities;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Core.Interfaces.VideoFramerExtractor;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace FiapX.Application.UseCases.VideoProcessing
{
    public class ProcessVideoUseCase : IProcessVideoUseCase
    {
        private readonly IVideoBatchRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _storageService;
        private readonly IVideoFrameExtractorService _frameExtractor;
        private readonly INotifyBatchProcessingResultUseCase _notificationUseCase;
        private readonly ILogger<ProcessVideoUseCase> _logger;

        public ProcessVideoUseCase(
            IVideoBatchRepository repository,
            IUnitOfWork unitOfWork,
            IFileStorageService storageService,
            IVideoFrameExtractorService frameExtractor,
            INotifyBatchProcessingResultUseCase notificationUseCase,
            ILogger<ProcessVideoUseCase> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _storageService = storageService;
            _frameExtractor = frameExtractor;
            _notificationUseCase = notificationUseCase;
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
                    _logger.LogWarning(ex, "Concurrency conflict detected for Video {inputVideoId}. Attempt {attempt}/{MaxRetries}.", input.VideoId, attempt, MaxRetries);

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

            var tempPath = Path.GetTempPath();
            var uniqueId = Guid.NewGuid();

            var originalExtension = Path.GetExtension(video.FileName);
            var inputVideoPath = Path.Combine(tempPath, $"{uniqueId}_input{originalExtension}");
            var outputFramesDir = Path.Combine(tempPath, $"{uniqueId}_frames");
            var zipFilePath = Path.Combine(tempPath, $"{uniqueId}_processed.zip");

            try
            {
                var blobName = $"{batch.Id}/{video.FileName}";
                using (var videoStream = await _storageService.DownloadFileAsync(blobName, "videos-raw"))
                using (var fileStream = File.Create(inputVideoPath))
                {
                    await videoStream.CopyToAsync(fileStream);
                }

                await _frameExtractor.ExtractFramesAsync(inputVideoPath, outputFramesDir);

                ZipFile.CreateFromDirectory(outputFramesDir, zipFilePath);

                string zipUrl;
                using (var zipStream = File.OpenRead(zipFilePath))
                {
                    var zipFileName = $"{batch.Id}/{video.Id}_processed.zip";
                    zipUrl = await _storageService.UploadFileAsync(zipStream, zipFileName, "videos-zip");
                }

                video.MarkAsDone(zipUrl);
                batch.UpdateStatus();
                _repository.Update(batch);
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is InvalidDataException)
                {
                    _logger.LogError(ex, "Erro de negócio (arquivo inválido) no vídeo {VideoId}.", input.VideoId);
                    video.MarkAsError(ex.Message);
                    batch.UpdateStatus();
                    _repository.Update(batch);
                    await _unitOfWork.CommitAsync();

                    await NotifyIfBatchFinishedAsync(batch, ex.Message);
                    return;
                }
                else
                {
                    _logger.LogError(ex, "Erro de infraestrutura no vídeo {VideoId}.", input.VideoId);
                    throw;
                }
            }
            finally
            {
                CleanupTempFiles(inputVideoPath, outputFramesDir, zipFilePath);
            }

            await _unitOfWork.CommitAsync();
            await NotifyIfBatchFinishedAsync(batch, string.Empty);
        }

        private async Task NotifyIfBatchFinishedAsync(VideoBatch batch, string errorMessage)
        {
            if (batch.Status == BatchStatus.Completed)
            {
                await _notificationUseCase.ExecuteSuccessAsync(batch.UserId, batch.Id);
            }
            else if (batch.Status == BatchStatus.Error)
            {
                await _notificationUseCase.ExecuteErrorAsync(batch.UserId, batch.Id, errorMessage);
            }
        }

        private void CleanupTempFiles(string videoPath, string framesDir, string zipPath)
        {
            try
            {
                if (File.Exists(videoPath)) File.Delete(videoPath);
                if (File.Exists(zipPath)) File.Delete(zipPath);
                if (Directory.Exists(framesDir)) Directory.Delete(framesDir, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível limpar arquivos temporários: {Message}", ex.Message);
            }
        }
    }
}