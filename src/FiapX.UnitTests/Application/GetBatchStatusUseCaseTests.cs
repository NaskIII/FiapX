using System;
using System.Linq;
using System.Threading.Tasks;
using FiapX.Application.UseCases.Batch;
using FiapX.Core.Entities;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace FiapX.UnitTests.Application;

public class GetBatchStatusUseCaseTests
{
    private readonly Mock<IVideoBatchRepository> _repoMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly GetBatchStatusUseCase _useCase;

    public GetBatchStatusUseCaseTests()
    {
        _repoMock = new Mock<IVideoBatchRepository>();
        _storageMock = new Mock<IFileStorageService>();

        _storageMock.Setup(s => s.GenerateSasToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                    .Returns("https://secure-url.com/video.zip?sig=token123");

        _useCase = new GetBatchStatusUseCase(_repoMock.Object, _storageMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Null_When_Batch_Not_Found()
    {
        _repoMock.Setup(r => r.GetBatchWithVideosAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((VideoBatch?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Generate_SasToken_When_Video_Is_Done_And_Url_Is_Absolute()
    {
        var batch = new VideoBatch(Guid.NewGuid());
        batch.AddVideo("test.mp4", "raw/url");
        var video = batch.Videos.First();

        string absoluteRawUrl = "https://azure.blob.core/videos-zip/my-video.zip";
        video.MarkAsDone(absoluteRawUrl);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batch.Id))
                 .ReturnsAsync(batch);

        var result = await _useCase.ExecuteAsync(batch.Id);

        result.Should().NotBeNull();
        result!.BatchId.Should().Be(batch.Id);
        result.Videos.Should().HaveCount(1);

        var videoDto = result.Videos.First();
        videoDto.Status.Should().Be(VideoStatus.Done.ToString());
        videoDto.DownloadUrl.Should().Be("https://secure-url.com/video.zip?sig=token123");

        _storageMock.Verify(s => s.GenerateSasToken("videos-zip", "my-video.zip", It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Null_DownloadUrl_When_Video_Is_Not_Done()
    {
        var batch = new VideoBatch(Guid.NewGuid());
        batch.AddVideo("test.mp4", "raw/url");

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batch.Id))
                 .ReturnsAsync(batch);

        var result = await _useCase.ExecuteAsync(batch.Id);

        var videoDto = result!.Videos.First();

        videoDto.Status.Should().NotBe(VideoStatus.Done.ToString());
        videoDto.DownloadUrl.Should().BeNull();

        _storageMock.Verify(s => s.GenerateSasToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_RawUrl_When_Url_Is_Not_Absolute_Uri()
    {
        var batch = new VideoBatch(Guid.NewGuid());
        batch.AddVideo("test.mp4", "raw/url");
        var video = batch.Videos.First();

        string relativeUrl = "zip/url.zip";
        video.MarkAsDone(relativeUrl);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batch.Id))
                 .ReturnsAsync(batch);

        var result = await _useCase.ExecuteAsync(batch.Id);

        var videoDto = result!.Videos.First();

        videoDto.Status.Should().Be(VideoStatus.Done.ToString());
        videoDto.DownloadUrl.Should().Be(relativeUrl);

        _storageMock.Verify(s => s.GenerateSasToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_RawUrl_When_Url_Does_Not_Contain_Container_Name()
    {
        var batch = new VideoBatch(Guid.NewGuid());
        batch.AddVideo("test.mp4", "raw/url");
        var video = batch.Videos.First();

        string urlWithoutContainer = "https://azure.blob.core/other-folder/my-video.zip";
        video.MarkAsDone(urlWithoutContainer);

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batch.Id))
                 .ReturnsAsync(batch);

        var result = await _useCase.ExecuteAsync(batch.Id);

        var videoDto = result!.Videos.First();

        videoDto.DownloadUrl.Should().Be(urlWithoutContainer);

        _storageMock.Verify(s => s.GenerateSasToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
}