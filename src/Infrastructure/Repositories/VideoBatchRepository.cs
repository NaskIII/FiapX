using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapX.Infrastructure.Repositories
{
    public class VideoBatchRepository : Repository<VideoBatch>, IVideoBatchRepository
    {
        public VideoBatchRepository(FiapXDbContext context) : base(context)
        {
        }

        public async Task<VideoBatch?> GetBatchWithVideosAsync(Guid batchId)
        {
            var batch = await _dbSet
                .WithPartitionKey(batchId.ToString())
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null) return null;

            var videos = await _context.Videos
                .WithPartitionKey(batchId.ToString())
                .Where(v => v.BatchId == batchId)
                .ToListAsync();

            if (videos.Count != 0)
            {
                var field = typeof(VideoBatch).GetField("_videos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(batch, videos);
            }

            return batch;
        }

        public async Task ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();

            await Task.CompletedTask;
        }
    }
}
