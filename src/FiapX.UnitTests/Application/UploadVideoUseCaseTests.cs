using FiapX.Application.UseCases.DTOs;
using FiapX.Application.UseCases.VideoUpload;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FiapX.Core.Interfaces.UnityOfWork;
using FluentAssertions;
using Moq;

namespace FiapX.UnitTests.Application;

public class UploadVideoUseCaseTests
{
    private readonly Mock<IVideoBatchRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly UploadVideoUseCase _useCase;

    public UploadVideoUseCaseTests()
    {
        _repoMock = new Mock<IVideoBatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _storageMock = new Mock<IFileStorageService>();
        _publisherMock = new Mock<IMessagePublisher>();
        _userContextMock = new Mock<IUserContext>();

        _userContextMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _userContextMock.Setup(u => u.IsAuthenticated).Returns(true);

        _useCase = new UploadVideoUseCase(
            _repoMock.Object,
            _uowMock.Object,
            _storageMock.Object,
            _publisherMock.Object,
            _userContextMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Multiple_Videos_In_Single_Batch()
    {
        var files = new List<FileInput>
        {
            new("video1.mp4", new MemoryStream(), "video/mp4"),
            new("video2.mov", new MemoryStream(), "video/quicktime")
        };

        var input = new UploadBatchInput(files);

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.Is<string>(f => f.Contains("video1")), "videos-raw"))
                    .ReturnsAsync("http://url/video1.mp4");

        _storageMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.Is<string>(f => f.Contains("video2")), "videos-raw"))
                    .ReturnsAsync("http://url/video2.mov");

        var result = await _useCase.ExecuteAsync(input);

        result.Should().NotBeNull();
        result.TotalVideos.Should().Be(2);

        _storageMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "videos-raw"), Times.Exactly(2));

        _repoMock.Verify(r => r.AddAsync(It.Is<VideoBatch>(b =>
            b.Videos.Count == 2 &&
            b.UserId != Guid.Empty
        )), Times.Once);

        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<object>(), "videos-processing"), Times.Exactly(2));
    }
}