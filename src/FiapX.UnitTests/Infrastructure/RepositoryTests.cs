using FiapX.Infrastructure.Data;
using FiapX.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FiapX.UnitTests.Infrastructure;

public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<TestChild> Children { get; set; } = new();
}

public class TestChild
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid TestEntityId { get; set; }
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
            
            entity.HasMany(e => e.Children)
                  .WithOne()
                  .HasForeignKey(c => c.TestEntityId);
        });

        modelBuilder.Entity<TestChild>(entity =>
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
    }

    [Fact]
    public async Task Update_Should_Modify_Entity()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };

        await context.Set<TestEntity>().AddAsync(entity);
        await context.SaveChangesAsync();

        entity.Name = "Modified";
        repository.Update(entity);
        await context.SaveChangesAsync();

        var updated = await context.Set<TestEntity>().FindAsync(entity.Id);
        Assert.Equal("Modified", updated!.Name);
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
    public async Task GetSingleAsync_Should_Return_Entity_By_Predicate()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "UniqueName" });
        await context.SaveChangesAsync();

        var result = await repository.GetSingleAsync(x => x.Name == "UniqueName");

        Assert.NotNull(result);
        Assert.Equal("UniqueName", result.Name);
    }

    [Fact]
    public async Task GetSingleAsync_With_Include_Expression_Should_Load_Children()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        var parentId = Guid.NewGuid();
        var parent = new TestEntity { Id = parentId, Name = "Parent" };
        var child = new TestChild { Id = Guid.NewGuid(), TestEntityId = parentId, Description = "Child" };

        parent.Children.Add(child);

        await context.Set<TestEntity>().AddAsync(parent);
        await context.SaveChangesAsync();

        var result = await repository.GetSingleAsync(
            x => x.Id == parentId,
            x => x.Children
        );

        Assert.NotNull(result);
        Assert.NotEmpty(result.Children);
        Assert.Equal("Child", result.Children.First().Description);
    }

    [Fact]
    public async Task GetManyAsync_Should_Return_List_By_Predicate()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddRangeAsync(
            new TestEntity { Id = Guid.NewGuid(), Active = true },
            new TestEntity { Id = Guid.NewGuid(), Active = true },
            new TestEntity { Id = Guid.NewGuid(), Active = false }
        );
        await context.SaveChangesAsync();

        var result = await repository.GetManyAsync(x => x.Active);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CountAsync_Should_Return_Total_Count()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddRangeAsync(
            new TestEntity { Id = Guid.NewGuid() },
            new TestEntity { Id = Guid.NewGuid() }
        );
        await context.SaveChangesAsync();

        var count = await repository.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_With_Predicate_Should_Return_Filtered_Count()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddRangeAsync(
            new TestEntity { Id = Guid.NewGuid(), Active = true },
            new TestEntity { Id = Guid.NewGuid(), Active = false }
        );
        await context.SaveChangesAsync();

        var count = await repository.CountAsync(x => x.Active);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_If_Exists()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Exists" });
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync(x => x.Name == "Exists");
        var notExists = await repository.ExistsAsync(x => x.Name == "Ghost");

        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task ExistsByIdAsync_Should_Return_True_If_Id_Exists()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var id = Guid.NewGuid();

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = id });
        await context.SaveChangesAsync();

        var exists = await repository.ExistsByIdAsync(id);
        var notExists = await repository.ExistsByIdAsync(Guid.NewGuid());

        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task AsNoTrackingListAsync_Should_Return_All_Entities()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = Guid.NewGuid() });
        await context.SaveChangesAsync();

        var result = await repository.AsNoTrackingListAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task AsQueryable_Should_Return_Queryable()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        var query = repository.AsQueryable();

        Assert.NotNull(query);
        Assert.IsAssignableFrom<IQueryable<TestEntity>>(query);
    }

    [Fact]
    public async Task IsActiveAsync_Should_Check_Active_Property()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);
        var id = Guid.NewGuid();

        await context.Set<TestEntity>().AddAsync(new TestEntity { Id = id, Active = true });
        await context.SaveChangesAsync();

        var result = await repository.IsActiveAsync(id);
        var notFound = await repository.IsActiveAsync(Guid.NewGuid());

        Assert.True(result);
        Assert.False(notFound);
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
        var id3 = Guid.NewGuid();

        await context.Set<TestEntity>().AddRangeAsync(
            new TestEntity { Id = id1 },
            new TestEntity { Id = Guid.NewGuid() },
            new TestEntity { Id = id3 }
        );
        await context.SaveChangesAsync();

        var result = await repository.GetManyByIdAsync(new List<Guid> { id1, id3 });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == id1);
        Assert.Contains(result, e => e.Id == id3);
    }

    [Fact]
    public void Dispose_Should_Dispose_Context()
    {
        var dbName = Guid.NewGuid().ToString();
        var context = CreateDbContext(dbName);
        var repository = new Repository<TestEntity>(context);

        repository.Dispose();

        Assert.Throws<ObjectDisposedException>(() => context.Add(new TestEntity()));
    }
}