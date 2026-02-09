using FiapX.Application.UseCases.Batch;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FiapX.UnitTests.Application;

public class GetBatchStatusUseCaseTests
{
    private readonly Mock<IVideoBatchRepository> _repoMock;
    private readonly GetBatchStatusUseCase _useCase;

    public GetBatchStatusUseCaseTests()
    {
        _repoMock = new Mock<IVideoBatchRepository>();
        _useCase = new GetBatchStatusUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Mapped_Dto_When_Batch_Exists()
    {
        var batch = new VideoBatch(new Guid());
        batch.AddVideo("teste.mp4", "raw/url");
        var video = batch.Videos.First();
        video.MarkAsDone("zip/url.zip");

        _repoMock.Setup(r => r.GetBatchWithVideosAsync(batch.Id))
                 .ReturnsAsync(batch);

        var result = await _useCase.ExecuteAsync(batch.Id);

        result.Should().NotBeNull();
        result!.BatchId.Should().Be(batch.Id);
        result.Videos.Should().HaveCount(1);

        var videoDto = result.Videos.First();
        videoDto.Status.Should().Be("Done");
        videoDto.DownloadUrl.Should().Be("zip/url.zip");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Null_When_Batch_Not_Found()
    {
        _repoMock.Setup(r => r.GetBatchWithVideosAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((VideoBatch?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}