using FiapX.Core.Entities;
using FiapX.Core.Enums;
using FluentAssertions;

namespace FiapX.UnitTests.Domain;

public class VideoBatchTests
{
    [Fact]
    public void AddVideo_Should_Add_Video_To_List_And_Set_Correct_BatchId()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        var fileName = "video1.mp4";
        var filePath = "raw/video1.mp4";

        batch.AddVideo(fileName, filePath);

        batch.Videos.Should().HaveCount(1);
        var addedVideo = batch.Videos.First();
        addedVideo.FileName.Should().Be(fileName);
        addedVideo.FilePath.Should().Be(filePath);
        addedVideo.BatchId.Should().Be(batch.Id);
        addedVideo.Status.Should().Be(VideoStatus.Pending);
    }

    [Fact]
    public void UpdateStatus_Should_Change_To_Completed_When_All_Videos_Are_Done()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.AddVideo("v1.mp4", "path/v1");
        batch.AddVideo("v2.mp4", "path/v2");

        var video1 = batch.Videos.First(v => v.FileName == "v1.mp4");
        var video2 = batch.Videos.First(v => v.FileName == "v2.mp4");

        video1.MarkAsDone("zip/v1.zip");
        video2.MarkAsDone("zip/v2.zip");

        batch.UpdateStatus();

        batch.Status.Should().Be(BatchStatus.Completed);
    }

    [Fact]
    public void UpdateStatus_Should_Change_To_Processing_When_At_Least_One_Video_Is_Processing()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.AddVideo("v1.mp4", "path/v1");
        batch.AddVideo("v2.mp4", "path/v2");

        var video2 = batch.Videos.First(v => v.FileName == "v2.mp4");
        video2.MarkAsProcessing();

        batch.UpdateStatus();

        batch.Status.Should().Be(BatchStatus.Processing);
    }

    [Fact]
    public void UpdateStatus_Should_Remain_Pending_If_All_Videos_Are_Pending()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.AddVideo("v1.mp4", "path/v1");
        batch.AddVideo("v2.mp4", "path/v2");

        batch.UpdateStatus();

        batch.Status.Should().Be(BatchStatus.Pending);
    }

    [Fact]
    public void UpdateStatus_Should_Change_To_Error_Only_If_All_Videos_Failed()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.AddVideo("v1.mp4", "path/v1");
        batch.AddVideo("v2.mp4", "path/v2");

        var video1 = batch.Videos.First(v => v.FileName == "v1.mp4");
        var video2 = batch.Videos.First(v => v.FileName == "v2.mp4");

        video1.MarkAsError("Format invalid");
        video2.MarkAsError("Timeout");

        batch.UpdateStatus();

        batch.Status.Should().Be(BatchStatus.Error);
    }

    [Fact]
    public void UpdateStatus_Should_Be_CompletedWithErrors_When_Some_Succeeded_And_Some_Failed()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.AddVideo("v1.mp4", "path/v1");
        batch.AddVideo("v2.mp4", "path/v2");

        var video1 = batch.Videos.First(v => v.FileName == "v1.mp4");
        video1.MarkAsDone("zip/v1.zip");

        var video2 = batch.Videos.First(v => v.FileName == "v2.mp4");
        video2.MarkAsError("File corrupted");

        batch.UpdateStatus();

        batch.Status.Should().Be(BatchStatus.CompletedWithErrors);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Pending_Status_And_Dates()
    {
        Guid userId = Guid.NewGuid();
        var batch = new VideoBatch(userId);

        batch.Id.Should().NotBeEmpty();
        batch.UserId.Should().Be(userId);
        batch.Status.Should().Be(BatchStatus.Pending);
        batch.Videos.Should().BeEmpty();
        batch.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}