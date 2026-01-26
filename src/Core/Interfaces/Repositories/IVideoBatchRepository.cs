using FiapX.Core.Entities;
using FiapX.Core.Interfaces.BaseRepository;

namespace FiapX.Core.Interfaces.Repositories
{
    public interface IVideoBatchRepository : IRepository<VideoBatch>
    {
        public Task<VideoBatch?> GetBatchWithVideosAsync(Guid batchId);
    }
}
