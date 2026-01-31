using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FiapX.Infrastructure.Services;
using Moq;

namespace FiapX.UnitTests.Infrastructure;

public class AzureBlobStorageServiceTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly Mock<BlobClient> _blobClientMock;
    private readonly AzureBlobStorageService _service;

    public AzureBlobStorageServiceTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _containerClientMock = new Mock<BlobContainerClient>();
        _blobClientMock = new Mock<BlobClient>();

        _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_containerClientMock.Object);

        _containerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        _service = new AzureBlobStorageService(_blobServiceClientMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_Should_CreateContainer_Upload_And_ReturnUri()
    {
        var fileName = "video.mp4";
        var containerName = "videos";
        var expectedUri = new Uri("http://fake-blob-storage/video.mp4");
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _blobClientMock.Setup(x => x.Uri).Returns(expectedUri);

        _blobClientMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var result = await _service.UploadFileAsync(stream, fileName, containerName);

        Assert.Equal(expectedUri.ToString(), result);

        _containerClientMock.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_ReturnStream_When_FileExists()
    {
        var fileName = "existente.mp4";
        var containerName = "videos";
        var expectedStream = new MemoryStream(new byte[] { 10, 20, 30 });

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, null!));

        _blobClientMock
            .Setup(x => x.OpenReadAsync(It.IsAny<long>(), It.IsAny<int?>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _service.DownloadFileAsync(fileName, containerName);

        Assert.NotNull(result);
        Assert.Equal(expectedStream.Length, result.Length);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_ThrowException_When_File_Not_Found()
    {
        var fileName = "fantasma.mp4";
        var containerName = "videos";

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, null!));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.DownloadFileAsync(fileName, containerName));
    }
}