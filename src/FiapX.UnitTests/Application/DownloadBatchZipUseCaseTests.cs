using FiapX.Application.UseCases.Batch;
using FiapX.Core.Entities;
using FiapX.Core.Enums;
using FiapX.Core.Interfaces;
using FiapX.Core.Interfaces.Repositories;
using Moq;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace FiapX.UnitTests.Application
{
    public class DownloadBatchZipUseCaseTests
    {
        private readonly Mock<IVideoBatchRepository> _batchRepositoryMock;
        private readonly Mock<IFileStorageService> _storageServiceMock;
        private readonly DownloadBatchZipUseCase _useCase;

        public DownloadBatchZipUseCaseTests()
        {
            _batchRepositoryMock = new Mock<IVideoBatchRepository>();
            _storageServiceMock = new Mock<IFileStorageService>();
            _useCase = new DownloadBatchZipUseCase(_batchRepositoryMock.Object, _storageServiceMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsUnauthorizedAccessException_WhenBatchDoesNotExist()
        {
            var batchId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            using var stream = new MemoryStream();

            _batchRepositoryMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
                .ReturnsAsync((VideoBatch?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _useCase.ExecuteAsync(batchId, userId, stream));
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsUnauthorizedAccessException_WhenUserIdDoesNotMatch()
        {
            var userId = Guid.NewGuid();
            var wrongUserId = Guid.NewGuid();
            using var stream = new MemoryStream();

            var batch = new VideoBatch(wrongUserId);
            var batchId = batch.Id;

            _batchRepositoryMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
                .ReturnsAsync(batch);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _useCase.ExecuteAsync(batchId, userId, stream));
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsFileNotFoundException_WhenNoCompletedVideosExist()
        {
            var userId = Guid.NewGuid();
            using var stream = new MemoryStream();

            var batch = new VideoBatch(userId);
            batch.AddVideo("video1.mp4", "http://original");
            var batchId = batch.Id;

            var video = batch.Videos.First();
            SetPrivatePropertyValue(video, "Status", VideoStatus.Processing);

            _batchRepositoryMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
                .ReturnsAsync(batch);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _useCase.ExecuteAsync(batchId, userId, stream));
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsFileNotFoundException_WhenStorageReturnsNullForValidVideos()
        {
            var userId = Guid.NewGuid();
            using var stream = new MemoryStream();

            var batch = new VideoBatch(userId);
            batch.AddVideo("video1.mp4", "http://original");
            var batchId = batch.Id;

            var video = batch.Videos.First();
            SetPrivatePropertyValue(video, "Status", VideoStatus.Done);
            SetPrivatePropertyValue(video, "OutputPath", "http://st.blob.core.windows.net/videos-zip/blob1_processed.zip");

            _batchRepositoryMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
                .ReturnsAsync(batch);

            _storageServiceMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Stream?)null);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _useCase.ExecuteAsync(batchId, userId, stream));
        }

        [Fact]
        public async Task ExecuteAsync_WritesToStream_WhenValidVideosAreProcessed()
        {
            var userId = Guid.NewGuid();
            using var outputStream = new MemoryStream();

            var batch = new VideoBatch(userId);
            batch.AddVideo("video1.mp4", "http://original");
            var batchId = batch.Id;

            var video = batch.Videos.First();
            SetPrivatePropertyValue(video, "Status", VideoStatus.Done);
            SetPrivatePropertyValue(video, "OutputPath", "http://st.blob.core.windows.net/videos-zip/blob1_processed.zip");

            _batchRepositoryMock.Setup(r => r.GetBatchWithVideosAsync(batchId))
                .ReturnsAsync(batch);

            var fakeFileBytes = Encoding.UTF8.GetBytes("fake zip content");
            var fakeFileStream = new MemoryStream(fakeFileBytes);

            _storageServiceMock.Setup(s => s.DownloadFileAsync("blob1_processed.zip", "videos-zip"))
                .ReturnsAsync(fakeFileStream);

            await _useCase.ExecuteAsync(batchId, userId, outputStream);

            Assert.True(outputStream.Length > 0);

            outputStream.Position = 0;
            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Read);
            Assert.Single(archive.Entries);
            Assert.Equal("video1.mp4.zip", archive.Entries[0].FullName);
        }

        private void SetPrivatePropertyValue<T>(T obj, string propertyName, object value)
        {
            var type = typeof(T);
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
                return;
            }

            var field = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
