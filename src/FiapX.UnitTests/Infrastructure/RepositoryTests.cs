using FiapX.Infrastructure.Data;
using FiapX.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiapX.UnitTests.Infrastructure;

public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TestDbContext : FiapXDbContext
{
    public TestDbContext(DbContextOptions<FiapXDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
    }
}

public class RepositoryTests
{
    private TestDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<FiapXDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task AddAsync_Should_Add_Entity_To_Context()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test 1" };

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        var storedEntity = await context.Set<TestEntity>().FindAsync(entity.Id);
        Assert.NotNull(storedEntity);
        Assert.Equal("Test 1", storedEntity.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Correct_Entity()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var id = Guid.NewGuid();

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = id, Name = "Target" });
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetPagedDataAsync_Should_Return_Correct_Page_And_Size()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        var entities = Enumerable.Range(1, 10).Select(i => new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Item {i}",
            CreatedAt = DateTime.UtcNow.AddMinutes(i)
        });

        await context.Set<TestEntity>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        var result = await repository.GetPagedDataAsync(
            x => true,
            pageNumber: 2,
            pageSize: 3,
            orderByProperty: "Name",
            orderByAscending: true
        );

        Assert.Equal(3, result.Count);

        var resultByDate = await repository.GetPagedDataAsync(
            x => true,
            pageNumber: 2,
            pageSize: 3,
            orderByProperty: "CreatedAt",
            orderByAscending: true
        );

        Assert.Equal(3, resultByDate.Count);
        Assert.Equal("Item 4", resultByDate[0].Name);
        Assert.Equal("Item 5", resultByDate[1].Name);
        Assert.Equal("Item 6", resultByDate[2].Name);
    }

    [Fact]
    public async Task IsActiveAsync_Should_Return_True_If_Active_Property_Is_True()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var id = Guid.NewGuid();

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = id, Active = true });
        await context.SaveChangesAsync();

        var result = await repository.IsActiveAsync(id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsActiveAsync_Should_Return_False_If_Entity_Not_Found()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        var result = await repository.IsActiveAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task Delete_Should_Remove_Entity()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid() };

        await context.Set<TestEntity>().AddAsync(entity);
        await context.SaveChangesAsync();

        repository.Delete(entity);
        await context.SaveChangesAsync();

        var deleted = await context.Set<TestEntity>().FindAsync(entity.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetManyByIdAsync_Should_Return_Filtered_List()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        await context.Set<TestEntity>().AddRangeAsync(
            new TestEntity { Id = id1 },
            new TestEntity { Id = id2 },
            new TestEntity { Id = id3 }
        );
        await context.SaveChangesAsync();

        var result = await repository.GetManyByIdAsync(new List<Guid> { id1, id3 });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == id1);
        Assert.Contains(result, e => e.Id == id3);
        Assert.DoesNotContain(result, e => e.Id == id2);
    }
}