using FiapX.Application;
using FiapX.Core.Interfaces.UnityOfWork;
using FiapX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                return await _context.SaveChangesAsync(cancellationToken) > 0;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("Conflito de concorrência detectado ao salvar dados.");
            }
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
