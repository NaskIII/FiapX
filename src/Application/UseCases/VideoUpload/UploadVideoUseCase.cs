using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FiapX.Core.Interfaces.UnityOfWork;

namespace FiapX.Application.UseCases.VideoUpload;

public class UploadVideoUseCase : IUploadVideoUseCase
{
    private readonly IVideoBatchRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _storageService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IUserContext _userContext;

    public UploadVideoUseCase(
        IVideoBatchRepository repository,
        IUnitOfWork unitOfWork,
        IFileStorageService storageService,
        IMessagePublisher messagePublisher,
        IUserContext userContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _messagePublisher = messagePublisher;
        _userContext = userContext;
    }

    public async Task<UploadBatchOutput> ExecuteAsync(UploadBatchInput input)
    {
        var userId = _userContext.UserId;

        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuário não identificado.");

        var batch = new VideoBatch(userId);

        foreach (var file in input.Files)
        {
            var storageFileName = $"{batch.Id}/{file.FileName}";

            var rawUrl = await _storageService.UploadFileAsync(file.FileStream, storageFileName, "videos-raw");

            batch.AddVideo(file.FileName, rawUrl);
        }

        await _repository.AddAsync(batch);
        await _unitOfWork.CommitAsync();

        foreach (var video in batch.Videos)
        {
            var message = new
            {
                BatchId = batch.Id,
                VideoId = video.Id,
                VideoUrl = video.FilePath,
                CreatedAt = DateTime.UtcNow
            };

            await _messagePublisher.PublishAsync(message, "videos-processing");
        }

        return new UploadBatchOutput(batch.Id, batch.Videos.Count, "Processing");
    }
}