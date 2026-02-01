using FiapX.Core.Entities;
using FiapX.Infrastructure.Repositories;
using FiapX.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FiapX.IntegrationTests.Repositories;

public class VideoBatchRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly VideoBatchRepository _repository;

    public VideoBatchRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new VideoBatchRepository(_fixture.Context);
    }

    [Fact]
    public async Task AddAsync_Should_Persist_Batch_In_CosmosDB()
    {
        var batch = new VideoBatch("user_integration@test.com");

        await _repository.AddAsync(batch);
        await _fixture.Context.SaveChangesAsync();

        var savedBatch = await _fixture.Context.VideoBatches
            .WithPartitionKey(batch.BatchId.ToString())
            .FirstOrDefaultAsync(b => b.Id == batch.Id);

        savedBatch.Should().NotBeNull();
        savedBatch!.UserOwner.Should().Be("user_integration@test.com");
    }

    [Fact]
    public async Task GetBatchWithVideosAsync_Should_Return_Batch_And_Children_Videos()
    {
        var batch = new VideoBatch("user_complex@test.com");
        batch.AddVideo("filme.mp4", "raw/filme.mp4");
        batch.AddVideo("clip.mp4", "raw/clip.mp4");

        await _repository.AddAsync(batch);
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.ChangeTracker.Clear();

        var result = await _repository.GetBatchWithVideosAsync(batch.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(batch.Id);
        result.Videos.Should().HaveCount(2);

        var video1 = result.Videos.FirstOrDefault(v => v.FileName == "filme.mp4");
        video1.Should().NotBeNull();
        video1!.BatchId.Should().Be(batch.Id);
    }
}