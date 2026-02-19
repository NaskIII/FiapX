using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FiapX.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapX.UnitTests.Infrastructure;

public class AzureBlobStorageServiceTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly Mock<BlobClient> _blobClientMock;
    private readonly Mock<ILogger<AzureBlobStorageService>> _loggerMock;
    private readonly AzureBlobStorageService _service;

    public AzureBlobStorageServiceTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _containerClientMock = new Mock<BlobContainerClient>();
        _blobClientMock = new Mock<BlobClient>();
        _loggerMock = new Mock<ILogger<AzureBlobStorageService>>();

        _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_containerClientMock.Object);

        _containerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        _service = new AzureBlobStorageService(_blobServiceClientMock.Object, _loggerMock.Object);
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

    [Fact]
    public void GenerateSasToken_Should_ReturnSasUri_When_CanGenerateSasUri_Is_True()
    {
        var containerName = "videos";
        var fileName = "video.mp4";
        var expectedSasUri = new Uri("https://fake-blob-storage/video.mp4?sig=assinatura-valida");

        _blobClientMock.Setup(x => x.CanGenerateSasUri).Returns(true);
        _blobClientMock.Setup(x => x.Name).Returns(fileName);
        _blobClientMock.Setup(x => x.BlobContainerName).Returns(containerName);

        _blobClientMock
            .Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
            .Returns(expectedSasUri);

        var result = _service.GenerateSasToken(containerName, fileName);

        Assert.Equal(expectedSasUri.ToString(), result);

        _blobClientMock.Verify(x => x.GenerateSasUri(It.Is<BlobSasBuilder>(b =>
            b.Permissions == "r" &&
            b.BlobName == fileName
        )), Times.Once);
    }

    [Fact]
    public void GenerateSasToken_Should_ReturnOriginalUri_And_LogWarning_When_CanGenerateSasUri_Is_False()
    {
        var containerName = "videos";
        var fileName = "video.mp4";
        var originalUri = new Uri("https://fake-blob-storage/video.mp4");

        _blobClientMock.Setup(x => x.CanGenerateSasUri).Returns(false);
        _blobClientMock.Setup(x => x.Uri).Returns(originalUri);

        var result = _service.GenerateSasToken(containerName, fileName);

        Assert.Equal(originalUri.ToString(), result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Não foi possível gerar SAS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GenerateSasToken_Should_ReturnEmptyString_And_LogError_When_Exception_Occurs()
    {
        var containerName = "videos";
        var fileName = "video.mp4";

        _blobClientMock.Setup(x => x.CanGenerateSasUri).Throws(new Exception("Erro de conexão simulado"));

        var result = _service.GenerateSasToken(containerName, fileName);

        Assert.Equal(string.Empty, result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao gerar SAS Token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}