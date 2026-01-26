using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Infrastructure.Data;

namespace FiapX.Infrastructure.BaseRepository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FiapXDbContext _context;

        public UnitOfWork(FiapXDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public Task BeginTransactionAsync()
        {
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync()
        {
            // No-op para Cosmos
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync()
        {
            return Task.CompletedTask;
        }
    }
}
