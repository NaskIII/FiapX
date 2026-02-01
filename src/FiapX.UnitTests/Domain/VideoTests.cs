using FiapX.Core.Entities;
using FiapX.Core.Enums;
using FluentAssertions;

namespace FiapX.UnitTests.Domain;

public class VideoTests
{
    [Fact]
    public void MarkAsDone_Should_Update_Status_And_Set_OutputPath()
    {
        var batchId = Guid.NewGuid();
        var video = new Video(batchId, "teste.mp4", "raw/teste.mp4");
        var zipPath = "zips/teste.zip";

        video.MarkAsDone(zipPath);

        video.Status.Should().Be(VideoStatus.Done);
        video.OutputPath.Should().Be(zipPath);
        video.ErrorMessage.Should().BeNull();
        video.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsError_Should_Update_Status_And_Set_ErrorMessage()
    {
        var batchId = Guid.NewGuid();
        var video = new Video(batchId, "teste.mp4", "raw/teste.mp4");
        var errorMsg = "Falha na conversão do FFmpeg";

        video.MarkAsError(errorMsg);

        video.Status.Should().Be(VideoStatus.Error);
        video.ErrorMessage.Should().Be(errorMsg);
        video.OutputPath.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_Should_Update_Status_Only()
    {
        var batchId = Guid.NewGuid();
        var video = new Video(batchId, "teste.mp4", "raw/teste.mp4");

        video.MarkAsProcessing();

        video.Status.Should().Be(VideoStatus.Processing);
    }
}