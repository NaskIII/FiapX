using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Infrastructure.Data;

namespace FiapX.Infrastructure.Repositories
{
    public class VideoRepository : Repository<Video>, IVideoRepository
    {
        public VideoRepository(FiapXDbContext context) : base(context)
        {
        }
    }
}
