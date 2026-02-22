using Azure.Storage.Blobs;
using FiapX.Infrastructure.Services;
using FiapX.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FiapX.IntegrationTests.Services;

public class BlobStorageServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly AzureBlobStorageService _service;
    private readonly string _containerName;

    public BlobStorageServiceTests(DatabaseFixture fixture, ILogger<AzureBlobStorageService> logger)
    {
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05);
        var serviceClient = new BlobServiceClient(fixture.Settings.Storage.ConnectionString, options);
        _service = new AzureBlobStorageService(serviceClient, logger);
        _containerName = fixture.Settings.Storage.ContainerRaw;
    }

    [Fact]
    public async Task Upload_And_Download_Should_Work_Correctly()
    {
        var fileName = $"test-video-{Guid.NewGuid()}.txt";
        var content = "Conteudo de teste simulando um video";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var uri = await _service.UploadFileAsync(stream, fileName, _containerName);

        uri.Should().NotBeNullOrEmpty();
        uri.Should().Contain(fileName);

        var downloadedStream = await _service.DownloadFileAsync(fileName, _containerName);

        using var reader = new StreamReader(downloadedStream);
        var downloadedContent = await reader.ReadToEndAsync();

        downloadedContent.Should().Be(content);
    }
}