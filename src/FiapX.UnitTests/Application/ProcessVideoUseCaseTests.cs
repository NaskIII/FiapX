using FiapX.Application;
using FiapX.Application.UseCases.DTOs;
using FiapX.Application.UseCases.VideoProcessing;
using FiapX.Core.Entities;
using FiapX.Core.Entities.Base;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Core.Interfaces.VideoFramerExtractor;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FiapX.UnitTests.Application;

public class ProcessVideoUseCaseTests
{
    private readonly Mock<IVideoBatchRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<IVideoFrameExtractorService> _frameExtractorMock;
    private readonly Mock<ILogger<ProcessVideoUseCase>> _loggerMock;
    private readonly ProcessVideoUseCase _useCase;

    public ProcessVideoUseCaseTests()
    {
        _repoMock = new Mock<IVideoBatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _storageMock = new Mock<IFileStorageService>();
        _frameExtractorMock = new Mock<IVideoFrameExtractorService>();
        _loggerMock = new Mock<ILogger<ProcessVideoUseCase>>();

        _useCase = new ProcessVideoUseCase(
            _repoMock.Object,
            _uowMock.Object,
            _storageMock.Object,
            _frameExtractorMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Process_Video_Successfully()
    {
        var batchId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var batch = new VideoBatch("user1");

        typeof(Entity).GetProperty("Id")?.SetValue(batch, batchId);

        batch.AddVideo("test.mp4", "path/test.mp4");
        var video = batch.Videos.First();

        typeof(Entity).GetProperty("Id")?.SetValue(video, videoId);

        var input = new ProcessVideoInput(batchId, videoId);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
            .ReturnsAsync(batch);

        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>(), "videos-raw"))
            .ReturnsAsync(new MemoryStream(new byte[100]));

        _frameExtractorMock.Setup(f => f.ExtractFramesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((inputFile, outputDir) =>
            {
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                File.WriteAllText(Path.Combine(outputDir, "frame_001.txt"), "fake content");
            })
            .Returns(Task.CompletedTask);

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "videos-zip"))
            .ReturnsAsync("http://blob/zip.zip");

        await _useCase.ExecuteAsync(input);

        Assert.Equal(VideoStatus.Done, video.Status);
        Assert.Equal("http://blob/zip.zip", video.OutputPath);

        _frameExtractorMock.Verify(f => f.ExtractFramesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _storageMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.Is<string>(n => n.Contains(".zip")), "videos-zip"), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Extractor_Error_Correctly()
    {
        var batchId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var input = new ProcessVideoInput(batchId, videoId);

        var batch = new VideoBatch("user1");
        typeof(Entity).GetProperty("Id")?.SetValue(batch, batchId);
        batch.AddVideo("test.mp4", "path/test.mp4");
        var video = batch.Videos.First();
        typeof(Entity).GetProperty("Id")?.SetValue(video, videoId);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId)).ReturnsAsync(batch);
        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>(), "videos-raw")).ReturnsAsync(new MemoryStream(new byte[10]));

        _frameExtractorMock.Setup(f => f.ExtractFramesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidDataException("Arquivo corrompido"));

        await _useCase.ExecuteAsync(input);

        Assert.Equal(VideoStatus.Error, video.Status);
        Assert.Equal("Arquivo corrompido", video.ErrorMessage);
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
            typeof(Entity).GetProperty("Id")?.SetValue(b, batchId);
            b.AddVideo("test.mp4", "path");
            var v = b.Videos.First();
            typeof(Entity).GetProperty("Id")?.SetValue(v, videoId);
            return b;
        }

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
            .ReturnsAsync(CreateFreshBatch);

        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>(), "videos-raw"))
            .ReturnsAsync(() => new MemoryStream(new byte[10]));

        _frameExtractorMock.Setup(f => f.ExtractFramesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((i, o) =>
            {
                Directory.CreateDirectory(o);
                File.WriteAllText(Path.Combine(o, "f.txt"), "x");
            })
            .Returns(Task.CompletedTask);

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "videos-zip"))
            .ReturnsAsync("http://url.com");

        _uowMock.SetupSequence(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Erro 1"))
            .ThrowsAsync(new ConcurrencyException("Erro 2"))
            .Returns(Task.FromResult(true));

        await _useCase.ExecuteAsync(input);

        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _repoMock.Verify(r => r.ClearChangeTracker(), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_If_Video_Already_Done()
    {
        var batchId = Guid.NewGuid();
        var batch = new VideoBatch("user1");
        batch.AddVideo("test.mp4", "path");
        var video = batch.Videos.First();
        video.MarkAsDone("http://old.zip");

        var input = new ProcessVideoInput(batchId, video.Id);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batchId)).ReturnsAsync(batch);

        await _useCase.ExecuteAsync(input);

        _storageMock.Verify(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _frameExtractorMock.Verify(f => f.ExtractFramesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}