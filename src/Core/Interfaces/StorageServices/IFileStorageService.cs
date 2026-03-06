namespace FiapX.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName);
    Task<Stream> DownloadFileAsync(string fileName, string containerName);
    string GenerateSasToken(string containerName, string fileName, int expirationMinutes = 60);
}