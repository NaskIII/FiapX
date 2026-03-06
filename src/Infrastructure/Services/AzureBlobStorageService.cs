using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using FiapX.Core.Interfaces;
using FiapX.Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace FiapX.Infrastructure.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient, ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(fileName);

        if (fileStream.Position > 0)
            fileStream.Position = 0;

        await blobClient.UploadAsync(fileStream, overwrite: true);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadFileAsync(string fileName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Arquivo {fileName} não encontrado no container {containerName}");
        }

        return await blobClient.OpenReadAsync();
    }

    public string GenerateSasToken(string containerName, string fileName, int expirationMinutes = 60)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
            {
                _logger.LogWarning($"Não foi possível gerar SAS para {fileName}. Verifique se o blobClient suporta SAS.");
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao gerar SAS Token para {fileName}");
            return string.Empty;
        }
    }
}