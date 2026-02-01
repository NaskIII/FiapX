using FiapX.Application;
using FiapX.Infrastructure.BaseRepository;
using FiapX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FiapX.UnitTests.Infrastructure;

public class UnitOfWorkTests
{
    private DbContextOptions<FiapXDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<FiapXDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CommitAsync_Should_Return_True_When_Changes_Are_Saved()
    {
        var options = CreateOptions();

        var contextMock = new Mock<FiapXDbContext>(options);

        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1);

        var uow = new UnitOfWork(contextMock.Object);

        var result = await uow.CommitAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task CommitAsync_Should_Return_False_When_No_Changes_Exist()
    {
        var options = CreateOptions();
        var contextMock = new Mock<FiapXDbContext>(options);

        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(0);

        var uow = new UnitOfWork(contextMock.Object);

        var result = await uow.CommitAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task CommitAsync_Should_Throw_ConcurrencyException_When_DbUpdateConcurrencyException_Occurs()
    {
        var options = CreateOptions();
        var contextMock = new Mock<FiapXDbContext>(options);

        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new DbUpdateConcurrencyException("Erro simulado"));

        var uow = new UnitOfWork(contextMock.Object);

        await Assert.ThrowsAsync<ConcurrencyException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task Transaction_Methods_Should_Complete_Successfully()
    {
        var options = CreateOptions();
        var contextMock = new Mock<FiapXDbContext>(options);
        var uow = new UnitOfWork(contextMock.Object);

        var exceptionBegin = await Record.ExceptionAsync(() => uow.BeginTransactionAsync());
        var exceptionCommit = await Record.ExceptionAsync(() => uow.CommitTransactionAsync());
        var exceptionRollback = await Record.ExceptionAsync(() => uow.RollbackTransactionAsync());

        Assert.Null(exceptionBegin);
        Assert.Null(exceptionCommit);
        Assert.Null(exceptionRollback);
    }
}