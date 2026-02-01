using FiapX.Infrastructure.Services;
using System.Reflection;

namespace FiapX.UnitTests.Infrastructure
{
    public class FFMpegFrameExtractorServiceTests
    {

        [Fact]
        public async Task ExtractFramesAsync_Should_Throw_FileNotFoundException_When_Binary_Missing()
        {
            var service = new FFMpegFrameExtractorService();
            var tempInputPath = Path.GetTempFileName();
            var outputDir = Path.Combine(Path.GetTempPath(), "frames_" + Guid.NewGuid());

            var field = typeof(FFMpegFrameExtractorService)
                .GetField("_ffmpegBinaryPath", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(service, "C:\\Caminho\\Inexistente\\ffmpeg.exe");

            try
            {
                var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                    service.ExtractFramesAsync(tempInputPath, outputDir));

                Assert.Contains("FFmpeg binary not found", exception.Message);
            }
            finally
            {
                if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public async Task ExtractFramesAsync_Should_Throw_InvalidDataException_When_FFMpeg_Fails()
        {
        }
    }
}