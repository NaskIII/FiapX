using FFMpegCore;
using FFMpegCore.Exceptions;
using FiapX.Core.Interfaces.VideoFramerExtractor;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FiapX.Infrastructure.Services
{
    public class FFMpegFrameExtractorService : IVideoFrameExtractorService
    {
        private readonly string _ffmpegBinaryPath;

        public FFMpegFrameExtractorService()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            var resourcesFolder = Path.Combine(assemblyDirectory!, "Resources");

            GlobalFFOptions.Configure(options =>
            {
                options.BinaryFolder = resourcesFolder;
                options.TemporaryFilesFolder = Path.GetTempPath();
            });

            var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            _ffmpegBinaryPath = Path.Combine(resourcesFolder, binaryName);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnsureLinuxExecutionPermission(_ffmpegBinaryPath);
            }
        }

        private static void EnsureLinuxExecutionPermission(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start("chmod", $"+x {filePath}").WaitForExit();
                }
            }
            catch
            {
                // Ignora falhas de permissão em dev
            }
        }

        public async Task ExtractFramesAsync(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                if (!Directory.Exists(outputDirectoryPath)) Directory.CreateDirectory(outputDirectoryPath);

                if (!File.Exists(_ffmpegBinaryPath))
                {
                    throw new FileNotFoundException($"FFmpeg binary not found at: {_ffmpegBinaryPath}");
                }

                var outputPathPattern = Path.Combine(outputDirectoryPath, "frame_%04d.png");

                await FFMpegArguments
                    .FromFileInput(inputFilePath)
                    .OutputToFile(outputPathPattern, true, options => options
                        .WithCustomArgument("-vsync 0"))
                    .ProcessAsynchronously();
            }
            catch (FFMpegException ex)
            {
                throw new InvalidDataException($"FFmpeg failure: {ex.Message}", ex);
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"FFmpeg executable not found or failed to start.", ex);
            }
        }
    }
}