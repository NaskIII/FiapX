using FiapX.Application;
using FiapX.Application.UseCases.DTOs;
using FiapX.Application.UseCases.VideoProcessing;
using FiapX.Core.Entities;
using FiapX.Core.Entities.Base;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.UnityOfWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FiapX.UnitTests.Application;

public class ProcessVideoUseCaseTests
{
    private readonly Mock<IVideoBatchRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<ILogger<ProcessVideoUseCase>> _loggerMock;
    private readonly ProcessVideoUseCase _useCase;

    public ProcessVideoUseCaseTests()
    {
        _repoMock = new Mock<IVideoBatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _storageMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<ProcessVideoUseCase>>();

        _useCase = new ProcessVideoUseCase(
            _repoMock.Object,
            _uowMock.Object,
            _storageMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Process_Video_Successfully()
    {
        var batchId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var input = new ProcessVideoInput(batchId, videoId);

        var batch = new VideoBatch("user1");
        
        batch.AddVideo("test.mp4", "path/test.mp4");

        var video = batch.Videos.First();
        var realVideoId = video.Id;
        input = new ProcessVideoInput(batchId, realVideoId);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
            .ReturnsAsync(batch);

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("http://blob/zip.zip");

        await _useCase.ExecuteAsync(input);

        Assert.Equal(VideoStatus.Done, video.Status);
        Assert.Equal("http://blob/zip.zip", video.OutputPath);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Retry_On_ConcurrencyException()
    {
        var batchId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var input = new ProcessVideoInput(batchId, videoId);

        VideoBatch CreateFreshBatch()
        {
            var b = new VideoBatch("user1");
            b.AddVideo("test.mp4", "path");
            var v = b.Videos.First();

            typeof(Entity)
                .GetProperty("Id")?
                .SetValue(v, videoId);

            return b;
        }

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
            .ReturnsAsync(() => CreateFreshBatch());

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("http://blob/zip.zip");

        _uowMock.SetupSequence(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Conflito simulado 1"))
            .ThrowsAsync(new ConcurrencyException("Conflito simulado 2"))
            .Returns(Task.FromResult(true));

        await _useCase.ExecuteAsync(input);

        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_After_MaxRetries()
    {
        var batchId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var input = new ProcessVideoInput(batchId, videoId);

        VideoBatch CreateFreshBatch()
        {
            var b = new VideoBatch("user1");
            b.AddVideo("test.mp4", "path");
            var v = b.Videos.First();

            typeof(Entity)
                .GetProperty("Id")?
                .SetValue(v, videoId);

            return b;
        }

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
            .ReturnsAsync(() => CreateFreshBatch());

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("http://blob/zip.zip");

        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Conflito eterno"));

        await Assert.ThrowsAsync<ConcurrencyException>(() => _useCase.ExecuteAsync(input));

        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_If_Video_Already_Done()
    {
        var batchId = Guid.NewGuid();
        var batch = new VideoBatch("user1");
        batch.AddVideo("test.mp4", "path");
        var video = batch.Videos.First();

        video.MarkAsDone("http://old-url.zip");

        var input = new ProcessVideoInput(batchId, video.Id);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId)).ReturnsAsync(batch);

        await _useCase.ExecuteAsync(input);

        _storageMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}